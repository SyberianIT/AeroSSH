namespace AeroSSH.Models;

public class SshSession
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public ServerProfile Profile { get; init; } = new();
    public DateTime ConnectedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    public string DisplayLabel => Profile.DisplayLabel;
}
