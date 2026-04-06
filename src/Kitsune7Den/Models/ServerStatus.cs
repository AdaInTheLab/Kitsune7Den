namespace Kitsune7Den.Models;

public enum ServerState
{
    Stopped,
    Starting,
    Running,
    Stopping
}

public class ServerStatus
{
    public ServerState State { get; set; } = ServerState.Stopped;
    public int PlayerCount { get; set; }
    public TimeSpan Uptime { get; set; }
    public DateTime? StartedAt { get; set; }
}
