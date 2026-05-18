namespace AeroSSH.Services;

public class HostKeyVerificationException : Exception
{
    public string Host { get; }
    public int Port { get; }
    public string ReceivedFingerprint { get; }
    public string? KnownFingerprint { get; }

    public bool IsMismatch => KnownFingerprint != null;
    public bool IsUnknownHost => KnownFingerprint == null;

    public HostKeyVerificationException(string host, int port, string received, string? known)
        : base(known == null
            ? $"Unknown host key for {host}:{port} ({received})"
            : $"Host key mismatch for {host}:{port}: expected {known}, got {received}")
    {
        Host = host;
        Port = port;
        ReceivedFingerprint = received;
        KnownFingerprint = known;
    }
}
