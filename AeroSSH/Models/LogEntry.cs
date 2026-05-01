namespace AeroSSH.Models
{
    /// <summary>Запись лога сессии</summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string SessionId { get; set; }
        public string Level { get; set; } // INFO, ERROR, WARNING, DEBUG
        public string Message { get; set; }
        public string Source { get; set; } // STDOUT, STDERR, SYSTEM
    }
}
