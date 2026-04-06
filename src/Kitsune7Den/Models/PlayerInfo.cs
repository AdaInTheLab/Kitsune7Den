namespace Kitsune7Den.Models;

public class PlayerInfo
{
    public int EntityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PlatformId { get; set; } = string.Empty;
    public string CrossId { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public int Health { get; set; }
    public int Deaths { get; set; }
    public int ZombieKills { get; set; }
    public int PlayerKills { get; set; }
    public int Score { get; set; }
    public int Level { get; set; }
    public int Ping { get; set; }
    public string Ip { get; set; } = string.Empty;
    public TimeSpan Playtime { get; set; }

    // Admin info (populated from AdminService)
    public bool IsAdmin { get; set; }
    public int PermissionLevel { get; set; } = 1000;
    public string PermissionLabel => PermissionLevel switch
    {
        0 => "Owner",
        1 => "Admin",
        2 => "Moderator",
        _ when PermissionLevel < 1000 => $"Level {PermissionLevel}",
        _ => ""
    };

    public string PositionText => $"{X:F0}, {Y:F0}, {Z:F0}";
}
