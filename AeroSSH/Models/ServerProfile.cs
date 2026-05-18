using System.Text.Json.Serialization;

namespace AeroSSH.Models;

public enum AuthMethod
{
    Password,
    PrivateKey
}

public class ServerProfile
{
    [JsonInclude]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 22;
    public string Username { get; set; } = string.Empty;

    public AuthMethod AuthMethod { get; set; } = AuthMethod.Password;

    public string? Password { get; set; }
    public byte[]? PrivateKey { get; set; }
    public string? KeyPassphrase { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

    public string DisplayLabel =>
        string.IsNullOrWhiteSpace(Name) ? $"{Username}@{Host}:{Port}" : Name;
}
