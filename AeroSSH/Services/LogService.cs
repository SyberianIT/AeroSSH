using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Android.Content;
using AeroSSH.Models;

namespace AeroSSH.Services;

public class LogService
{
    private readonly ConcurrentDictionary<string, List<LogEntry>> _bySession = new();
    private readonly object _writeLock = new();
    private long _sequence;
    private readonly string _logDir;

    public event EventHandler<LogEntry>? EntryAdded;

    public LogService(Context context)
    {
        _logDir = Path.Combine(context.FilesDir!.AbsolutePath, "logs");
        Directory.CreateDirectory(_logDir);
    }

    public LogEntry Add(string sessionId, string message, LogLevel level = LogLevel.Info, LogSource source = LogSource.System)
    {
        var entry = new LogEntry
        {
            Sequence = Interlocked.Increment(ref _sequence),
            SessionId = sessionId,
            Level = level,
            Source = source,
            Message = message
        };

        var bucket = _bySession.GetOrAdd(sessionId, _ => new List<LogEntry>());
        lock (bucket) bucket.Add(entry);

        EntryAdded?.Invoke(this, entry);
        return entry;
    }

    public IReadOnlyList<LogEntry> Get(string sessionId)
    {
        if (!_bySession.TryGetValue(sessionId, out var bucket)) return Array.Empty<LogEntry>();
        lock (bucket) return bucket.OrderBy(x => x.Sequence).ToList();
    }

    public void Clear(string sessionId)
    {
        if (_bySession.TryGetValue(sessionId, out var bucket))
            lock (bucket) bucket.Clear();
    }

    public async Task<string> ExportAsync(string sessionId, CancellationToken ct = default)
    {
        var entries = Get(sessionId);
        var path = Path.Combine(_logDir, $"session_{sessionId}_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, entries, new JsonSerializerOptions { WriteIndented = true }, ct);
        return path;
    }

    public async Task<string> ExportAsTextAsync(string sessionId, CancellationToken ct = default)
    {
        var entries = Get(sessionId);
        var path = Path.Combine(_logDir, $"session_{sessionId}_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        var sb = new StringBuilder();
        foreach (var e in entries)
            sb.Append('[').Append(e.Timestamp.ToString("HH:mm:ss")).Append("] ")
              .Append(e.Level).Append(' ').Append(e.Source).Append(": ")
              .AppendLine(e.Message);
        await File.WriteAllTextAsync(path, sb.ToString(), ct);
        return path;
    }
}
