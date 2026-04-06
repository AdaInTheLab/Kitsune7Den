using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Kitsune7Den.Models;

namespace Kitsune7Den.Services;

public partial class TelnetService : IDisposable
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _readCts;
    private readonly AppSettings _settings;

    public event Action<string>? DataReceived;
    public event Action<bool>? ConnectionStateChanged;

    public bool IsConnected => _client?.Connected == true;

    public TelnetService(AppSettings settings)
    {
        _settings = settings;
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            Disconnect();

            _client = new TcpClient();
            await _client.ConnectAsync("127.0.0.1", _settings.TelnetPort);
            _stream = _client.GetStream();

            _readCts = new CancellationTokenSource();
            _ = ReadLoopAsync(_readCts.Token);

            // Send password if configured
            if (!string.IsNullOrEmpty(_settings.TelnetPassword))
            {
                await Task.Delay(500); // wait for server prompt
                await SendCommandAsync(_settings.TelnetPassword);
            }

            ConnectionStateChanged?.Invoke(true);
            return true;
        }
        catch
        {
            ConnectionStateChanged?.Invoke(false);
            return false;
        }
    }

    public void Disconnect()
    {
        _readCts?.Cancel();
        _readCts?.Dispose();
        _readCts = null;

        _stream?.Dispose();
        _stream = null;

        _client?.Dispose();
        _client = null;

        ConnectionStateChanged?.Invoke(false);
    }

    public async Task<string> SendCommandAsync(string command)
    {
        if (_stream is null || !IsConnected)
            return string.Empty;

        var bytes = Encoding.UTF8.GetBytes(command + "\n");
        await _stream.WriteAsync(bytes);
        await Task.Delay(200); // brief wait for response
        return command;
    }

    public async Task<List<PlayerInfo>> GetPlayersAsync()
    {
        if (!IsConnected) return [];

        _lastPlayerList = [];
        await SendCommandAsync("listplayers");
        await Task.Delay(2000); // wait for full response

        return _lastPlayerList;
    }

    private List<PlayerInfo> _lastPlayerList = [];

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[4096];
        var sb = new StringBuilder();

        try
        {
            while (!ct.IsCancellationRequested && _stream is not null)
            {
                var bytesRead = await _stream.ReadAsync(buffer, ct);
                if (bytesRead == 0) break;

                var text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                sb.Append(text);

                // Process complete lines
                var content = sb.ToString();
                var lastNewline = content.LastIndexOf('\n');
                if (lastNewline >= 0)
                {
                    var complete = content[..(lastNewline + 1)];
                    sb.Clear();
                    sb.Append(content[(lastNewline + 1)..]);

                    foreach (var line in complete.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var trimmed = line.TrimEnd('\r');
                        ParsePlayerLine(trimmed);
                        DataReceived?.Invoke(trimmed);
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            ConnectionStateChanged?.Invoke(false);
        }
    }

    private void ParsePlayerLine(string line)
    {
        // 7D2D listplayers format (may be prefixed with timestamp from telnet):
        // 0. id=171, NonToxThicc, pos=(-303.4, 39.1, -1201.6), rot=(...), remote=True, health=96, deaths=0, zombies=38, players=0, score=37, level=3, ...
        // Total of 1 in the game

        // Strip timestamp prefix if present (e.g. "2026-04-06T10:30:04 176.012 INF ")
        var stripped = TimestampPrefixRegex().Replace(line, "");

        if (stripped.Contains("Total of ") && stripped.Contains(" in the game"))
        {
            // Don't clear — we build up the list as lines arrive
            return;
        }

        // Also detect "Executing command 'listplayers'" to reset the list
        if (stripped.Contains("Executing command 'listplayers'"))
        {
            _lastPlayerList = [];
            return;
        }

        var match = PlayerLineRegex().Match(stripped);
        if (match.Success)
        {
            var player = new PlayerInfo
            {
                EntityId = int.TryParse(match.Groups["id"].Value, out var id) ? id : 0,
                Name = match.Groups["name"].Value,
                X = float.TryParse(match.Groups["x"].Value, out var x) ? x : 0,
                Y = float.TryParse(match.Groups["y"].Value, out var y) ? y : 0,
                Z = float.TryParse(match.Groups["z"].Value, out var z) ? z : 0
            };

            // Parse additional fields from the rest of the line
            var rest = match.Groups["rest"].Value;
            if (ExtractField(rest, "health") is { } h && int.TryParse(h, out var health)) player.Health = health;
            if (ExtractField(rest, "deaths") is { } d && int.TryParse(d, out var deaths)) player.Deaths = deaths;
            if (ExtractField(rest, "zombies") is { } z2 && int.TryParse(z2, out var zk)) player.ZombieKills = zk;
            if (ExtractField(rest, "players") is { } pk && int.TryParse(pk, out var pkills)) player.PlayerKills = pkills;
            if (ExtractField(rest, "score") is { } sc && int.TryParse(sc, out var score)) player.Score = score;
            if (ExtractField(rest, "level") is { } lv && int.TryParse(lv, out var level)) player.Level = level;
            if (ExtractField(rest, "ip") is { } ip) player.Ip = ip;
            if (ExtractField(rest, "ping") is { } pg && int.TryParse(pg, out var ping)) player.Ping = ping;
            if (ExtractField(rest, "pltfmid") is { } pid) player.PlatformId = pid;
            if (ExtractField(rest, "crossid") is { } cid) player.CrossId = cid;

            _lastPlayerList.Add(player);
        }
    }

    private static string? ExtractField(string text, string fieldName)
    {
        var pattern = fieldName + "=";
        var idx = text.IndexOf(pattern, StringComparison.Ordinal);
        if (idx < 0) return null;
        var start = idx + pattern.Length;
        var end = text.IndexOf(',', start);
        return end < 0 ? text[start..].Trim() : text[start..end].Trim();
    }

    // Matches: 0. id=171, NonToxThicc, pos=(-303.4, 39.1, -1201.6), <rest...>
    [GeneratedRegex(@"^(\d+)\.\s+id=(?<id>\d+),\s+(?<name>[^,]+),\s+pos=\((?<x>[\d.-]+),\s*(?<y>[\d.-]+),\s*(?<z>[\d.-]+)\)(?<rest>.*)")]
    private static partial Regex PlayerLineRegex();

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T[\d:]+\s+[\d.]+\s+\w+\s+")]
    private static partial Regex TimestampPrefixRegex();

    public void Dispose()
    {
        Disconnect();
    }
}
