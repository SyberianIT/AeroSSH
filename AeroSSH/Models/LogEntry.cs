namespace AeroSSH.Models;

public enum LogLevel { Debug, Info, Warning, Error }
public enum LogSource { System, Stdout, Stderr, Command }

public class LogEntry
{
    public long Sequence { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string SessionId { get; init; } = string.Empty;
    public LogLevel Level { get; init; } = LogLevel.Info;
    public LogSource Source { get; init; } = LogSource.System;
    public string Message { get; init; } = string.Empty;
}
