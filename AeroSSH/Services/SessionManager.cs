using System.Collections.Concurrent;
using AeroSSH.Models;

namespace AeroSSH.Services;

public class SessionManager
{
    private readonly HostKeyStore _hostKeys;
    private readonly ConcurrentDictionary<string, SessionEntry> _sessions = new();

    public event EventHandler? SessionsChanged;

    public SessionManager(HostKeyStore hostKeys)
    {
        _hostKeys = hostKeys;
    }

    public IReadOnlyList<SshSession> ActiveSessions =>
        _sessions.Values.Select(e => e.Session).ToList();

    public async Task<SshSession> ConnectAsync(ServerProfile profile, CancellationToken ct)
    {
        var service = new SshServiceImpl(profile, _hostKeys);
        try
        {
            await service.ConnectAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            service.Dispose();
            throw;
        }

        var session = new SshSession { Profile = profile };
        _sessions[session.Id] = new SessionEntry(session, service);
        SessionsChanged?.Invoke(this, EventArgs.Empty);
        return session;
    }

    public ISshService? GetService(string sessionId) =>
        _sessions.TryGetValue(sessionId, out var entry) ? entry.Service : null;

    public SshSession? GetSession(string sessionId) =>
        _sessions.TryGetValue(sessionId, out var entry) ? entry.Session : null;

    public async Task DisconnectAsync(string sessionId)
    {
        if (!_sessions.TryRemove(sessionId, out var entry)) return;
        try { await entry.Service.DisconnectAsync().ConfigureAwait(false); }
        finally { entry.Service.Dispose(); }
        SessionsChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task DisconnectAllAsync()
    {
        var ids = _sessions.Keys.ToList();
        foreach (var id in ids) await DisconnectAsync(id).ConfigureAwait(false);
    }

    private record SessionEntry(SshSession Session, ISshService Service);
}
