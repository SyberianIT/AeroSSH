using AeroSSH.Models;
using System.Collections.Concurrent;

namespace AeroSSH.Services
{
    /// <summary>Менеджер множественных SSH сессий</summary>
    public class SessionManager
    {
        private readonly ConcurrentDictionary<string, (ISshService service, SshSession session)> _sessions = new();
        private readonly IPreferences _prefs;

        public SessionManager()
        {
            _prefs = SecureStorage.Default;
        }

        public async Task<string> CreateSessionAsync(SshSession session)
        {
            var service = new SshServiceImpl();
            var progress = new Progress<string>(msg => System.Diagnostics.Debug.WriteLine(msg));
            
            if (await service.ConnectAsync(session, progress, CancellationToken.None))
            {
                _sessions.TryAdd(session.Id, (service, session));
                SaveSession(session);
                return session.Id;
            }
            return null;
        }

        public ISshService GetService(string sessionId) =>
            _sessions.TryGetValue(sessionId, out var item) ? item.service : null;

        public SshSession GetSession(string sessionId) =>
            _sessions.TryGetValue(sessionId, out var item) ? item.session : null;

        public IEnumerable<SshSession> GetAllSessions() =>
            _sessions.Values.Select(x => x.session);

        public async Task RemoveSessionAsync(string sessionId)
        {
            if (_sessions.TryRemove(sessionId, out var item))
            {
                await item.service.DisconnectAsync();
                item.service.Dispose();
            }
        }

        private void SaveSession(SshSession session)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(session);
            SecureStorage.SetAsync($"session_{session.Id}", json);
        }
    }
}
