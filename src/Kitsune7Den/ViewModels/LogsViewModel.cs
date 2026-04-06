using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kitsune7Den.Models;

namespace Kitsune7Den.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    private readonly AppSettings _settings;

    [ObservableProperty] private LogFileItem? _selectedLog;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private string _filterText = "";

    public ObservableCollection<LogFileItem> LogFiles { get; } = [];
    public ObservableCollection<string> Lines { get; } = [];

    public LogsViewModel(AppSettings settings)
    {
        _settings = settings;
    }

    [RelayCommand]
    private void Refresh()
    {
        LogFiles.Clear();

        if (string.IsNullOrEmpty(_settings.ServerDirectory))
        {
            StatusMessage = "Server directory not set";
            return;
        }

        var serverDir = _settings.ServerDirectory;

        // Find all log files
        var patterns = new[] { "output_log_dedi__*.txt", "output_log*.txt" };
        var found = new HashSet<string>();

        foreach (var pattern in patterns)
        {
            foreach (var file in Directory.GetFiles(serverDir, pattern))
            {
                if (found.Add(file))
                {
                    var fi = new FileInfo(file);
                    LogFiles.Add(new LogFileItem
                    {
                        FileName = fi.Name,
                        FilePath = fi.FullName,
                        SizeBytes = fi.Length,
                        LastModified = fi.LastWriteTime
                    });
                }
            }
        }

        // Sort newest first
        var sorted = LogFiles.OrderByDescending(l => l.LastModified).ToList();
        LogFiles.Clear();
        foreach (var l in sorted)
            LogFiles.Add(l);

        if (LogFiles.Count > 0)
        {
            SelectedLog = LogFiles[0]; // auto-select newest
            StatusMessage = $"{LogFiles.Count} log files found";
        }
        else
        {
            StatusMessage = "No log files found";
        }
    }

    partial void OnSelectedLogChanged(LogFileItem? value)
    {
        if (value is not null)
            LoadLogFile(value);
    }

    partial void OnFilterTextChanged(string value)
    {
        if (SelectedLog is not null)
            LoadLogFile(SelectedLog);
    }

    private void LoadLogFile(LogFileItem logFile)
    {
        Lines.Clear();

        if (!File.Exists(logFile.FilePath))
        {
            StatusMessage = "File not found";
            return;
        }

        try
        {
            using var fs = new FileStream(logFile.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);

            var hasFilter = !string.IsNullOrWhiteSpace(FilterText);
            var filter = FilterText?.Trim() ?? "";
            var lineCount = 0;

            while (reader.ReadLine() is { } line)
            {
                if (hasFilter && !line.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    continue;

                Lines.Add(line);
                lineCount++;

                // Cap at 10000 lines for performance
                if (lineCount >= 10000)
                {
                    Lines.Add($"--- Showing first 10,000 of {logFile.FileName} (use filter to narrow) ---");
                    break;
                }
            }

            StatusMessage = hasFilter
                ? $"{lineCount} matching lines in {logFile.FileName}"
                : $"{lineCount} lines — {logFile.SizeText}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error reading log: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearFilter()
    {
        FilterText = "";
    }

    [RelayCommand]
    private void OpenInExplorer()
    {
        if (SelectedLog is null) return;
        try
        {
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{SelectedLog.FilePath}\"");
        }
        catch { /* best effort */ }
    }
}

public class LogFileItem
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime LastModified { get; set; }

    public string SizeText => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
        _ => $"{SizeBytes / (1024.0 * 1024):F1} MB"
    };

    public string DisplayName => $"{FileName}  ({SizeText}, {LastModified:MM/dd HH:mm})";

    public override string ToString() => DisplayName;
}
