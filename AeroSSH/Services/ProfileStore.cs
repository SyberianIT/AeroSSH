using System.Text.Json;
using AeroSSH.Models;

namespace AeroSSH.Services;

public class ProfileStore
{
    private const string KeyPrefix = "profile_";
    private readonly IKeyValueStore _prefs;
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public ProfileStore(IKeyValueStore prefs)
    {
        _prefs = prefs;
    }

    public IReadOnlyList<ServerProfile> GetAll()
    {
        var result = new List<ServerProfile>();
        foreach (var key in _prefs.KeysWithPrefix(KeyPrefix))
        {
            var json = _prefs.GetString(key);
            if (string.IsNullOrEmpty(json)) continue;
            try
            {
                var profile = JsonSerializer.Deserialize<ServerProfile>(json, JsonOpts);
                if (profile != null) result.Add(profile);
            }
            catch (JsonException)
            {
                // skip corrupted entries
            }
        }
        return result.OrderByDescending(p => p.LastUsedAt).ToList();
    }

    public ServerProfile? Get(string id)
    {
        var json = _prefs.GetString(KeyPrefix + id);
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<ServerProfile>(json, JsonOpts); }
        catch (JsonException) { return null; }
    }

    public void Save(ServerProfile profile)
    {
        var json = JsonSerializer.Serialize(profile, JsonOpts);
        _prefs.PutString(KeyPrefix + profile.Id, json);
    }

    public void TouchLastUsed(string id)
    {
        var profile = Get(id);
        if (profile == null) return;
        profile.LastUsedAt = DateTime.UtcNow;
        Save(profile);
    }

    public void Delete(string id) => _prefs.Remove(KeyPrefix + id);
}
