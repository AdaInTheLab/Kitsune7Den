namespace Kitsune7Den.Models;

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "INF";
    public string Message { get; set; } = string.Empty;
    public string RawLine { get; set; } = string.Empty;
}
