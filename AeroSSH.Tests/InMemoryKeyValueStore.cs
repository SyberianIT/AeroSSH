using AeroSSH.Services;

namespace AeroSSH.Tests;

public class InMemoryKeyValueStore : IKeyValueStore
{
    private readonly Dictionary<string, string> _data = new();

    public string? GetString(string key) => _data.TryGetValue(key, out var v) ? v : null;
    public void PutString(string key, string value) => _data[key] = value;
    public void Remove(string key) => _data.Remove(key);
    public IEnumerable<string> KeysWithPrefix(string prefix) =>
        _data.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList();
}
