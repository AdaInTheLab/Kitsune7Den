using System.IO;
using System.Text.RegularExpressions;
using Kitsune7Den.Models;

namespace Kitsune7Den.Services;

public partial class LogWatcherService : IDisposable
{
    private FileSystemWatcher? _watcher;
    private long _lastPosition;
    private string? _currentLogPath;
    private CancellationTokenSource? _pollCts;

    public event Action<LogEntry>? LogEntryReceived;

    /// <summary>
    /// Watch a specific log file (used when we know the exact file from server launch).
    /// </summary>
    public void StartWatchingFile(string logFilePath)
    {
        Stop();

        _currentLogPath = logFilePath;

        // File may not exist yet — server is still starting
        _lastPosition = File.Exists(logFilePath) ? new FileInfo(logFilePath).Length : 0;

        var logDir = Path.GetDirectoryName(logFilePath)!;
        var fileName = Path.GetFileName(logFilePath);
        _watcher = new FileSystemWatcher(logDir)
        {
            Filter = fileName,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };
        _watcher.Changed += (_, _) => ReadNewLines();
        _watcher.Created += (_, _) => ReadNewLines();

        // Poll as fallback
        _pollCts = new CancellationTokenSource();
        _ = PollLoopAsync(_pollCts.Token);
    }

    /// <summary>
    /// Watch a server directory, auto-detecting the latest log file.
    /// </summary>
    public void StartWatching(string serverDirectory)
    {
        Stop();

        _currentLogPath = FindLatestLogFile(serverDirectory);
        if (_currentLogPath is null) return;

        // Seek to end of file so we only see new output
        var fi = new FileInfo(_currentLogPath);
        _lastPosition = fi.Length;

        var logDir = Path.GetDirectoryName(_currentLogPath)!;
        _watcher = new FileSystemWatcher(logDir)
        {
            Filter = Path.GetFileName(_currentLogPath),
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += (_, _) => ReadNewLines();

        // Also poll every 2 seconds as a fallback
        _pollCts = new CancellationTokenSource();
        _ = PollLoopAsync(_pollCts.Token);
    }

    public void Stop()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();
        _pollCts = null;
        _watcher?.Dispose();
        _watcher = null;
        _currentLogPath = null;
        _lastPosition = 0;
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(2000, ct);
                ReadNewLines();
            }
        }
        catch (OperationCanceledException) { }
    }

    private void ReadNewLines()
    {
        if (_currentLogPath is null || !File.Exists(_currentLogPath))
            return;

        try
        {
            using var fs = new FileStream(_currentLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (fs.Length <= _lastPosition) return;

            fs.Seek(_lastPosition, SeekOrigin.Begin);
            using var reader = new StreamReader(fs);

            while (reader.ReadLine() is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var entry = ParseLogLine(line);
                LogEntryReceived?.Invoke(entry);
            }

            _lastPosition = fs.Position;
        }
        catch
        {
            // File may be locked briefly during write
        }
    }

    private static LogEntry ParseLogLine(string line)
    {
        // 7D2D log format: 2024-01-15T12:34:56 1234.567 INF Some message
        var match = LogLineRegex().Match(line);
        if (match.Success)
        {
            return new LogEntry
            {
                Timestamp = DateTime.TryParse(match.Groups["ts"].Value, out var ts) ? ts : DateTime.Now,
                Level = match.Groups["level"].Value,
                Message = match.Groups["msg"].Value,
                RawLine = line
            };
        }

        return new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = "INF",
            Message = line,
            RawLine = line
        };
    }

    private static string? FindLatestLogFile(string serverDirectory)
    {
        // First check for our timestamped log files
        var ourLogs = Directory.GetFiles(serverDirectory, "output_log_dedi__*.txt")
            .OrderByDescending(File.GetLastWriteTime)
            .FirstOrDefault();
        if (ourLogs is not null) return ourLogs;

        // Fallback: standard 7D2D log locations
        var candidates = new[]
        {
            Path.Combine(serverDirectory, "7DaysToDieServer_Data", "output_log.txt"),
            Path.Combine(serverDirectory, "output_log.txt"),
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path)) return path;
        }

        // Last resort: any .txt log in the directory
        var anyLog = Directory.GetFiles(serverDirectory, "output_log*.txt")
            .OrderByDescending(File.GetLastWriteTime)
            .FirstOrDefault();
        return anyLog;
    }

    [GeneratedRegex(@"^(?<ts>\d{4}-\d{2}-\d{2}T[\d:]+)\s+[\d.]+\s+(?<level>\w+)\s+(?<msg>.+)$")]
    private static partial Regex LogLineRegex();

    public void Dispose()
    {
        Stop();
    }
}
