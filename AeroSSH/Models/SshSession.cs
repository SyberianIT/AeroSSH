namespace AeroSSH.Models
{
    /// <summary>SSH сессия с метаданными подключения</summary>
    public class SshSession
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public byte[] PrivateKey { get; set; }
        public string KeyPassphrase { get; set; }
        public bool IsConnected { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
        public List<string> CommandHistory { get; set; } = new();
    }
}
