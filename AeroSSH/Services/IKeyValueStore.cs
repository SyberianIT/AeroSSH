namespace AeroSSH.Services;

public interface IKeyValueStore
{
    string? GetString(string key);
    void PutString(string key, string value);
    void Remove(string key);
    IEnumerable<string> KeysWithPrefix(string prefix);
}
