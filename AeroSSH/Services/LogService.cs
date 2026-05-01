using AeroSSH.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace AeroSSH.Services
{
    /// <summary>Сервис логирования сессий</summary>
    public class LogService
    {
        private readonly ConcurrentBag<LogEntry> _logs = new();
        private readonly string _logDir = Path.Combine(FileSystem.AppDataDirectory, "logs");

        public LogService()
        {
            Directory.CreateDirectory(_logDir);
        }

        public void Log(string sessionId, string message, string level = "INFO", string source = "SYSTEM")
        {
            var entry = new LogEntry
            {
                SessionId = sessionId,
                Message = message,
                Level = level,
                Source = source
            };
            _logs.Add(entry);
        }

        public IEnumerable<LogEntry> GetLogs(string sessionId) =>
            _logs.Where(x => x.SessionId == sessionId).OrderBy(x => x.Timestamp);

        public async Task ExportLogsAsync(string sessionId)
        {
            var logs = GetLogs(sessionId).ToList();
            var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
            var path = Path.Combine(_logDir, $"session_{sessionId}_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            await File.WriteAllTextAsync(path, json);
        }
    }
}
