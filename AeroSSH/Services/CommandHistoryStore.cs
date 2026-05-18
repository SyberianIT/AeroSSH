namespace AeroSSH.Services;

public class CommandHistoryStore
{
    private const string KeyPrefix = "history_";
    private const int MaxEntries = 100;
    private const string Separator = "";

    private readonly IKeyValueStore _store;

    public CommandHistoryStore(IKeyValueStore store)
    {
        _store = store;
    }

    public IReadOnlyList<string> Get(string profileId)
    {
        var raw = _store.GetString(KeyPrefix + profileId);
        if (string.IsNullOrEmpty(raw)) return Array.Empty<string>();
        return raw.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
    }

    public void Add(string profileId, string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return;
        var list = Get(profileId).ToList();
        list.RemoveAll(x => string.Equals(x, command, StringComparison.Ordinal));
        list.Insert(0, command);
        if (list.Count > MaxEntries) list = list.Take(MaxEntries).ToList();
        _store.PutString(KeyPrefix + profileId, string.Join(Separator, list));
    }

    public void Clear(string profileId) => _store.Remove(KeyPrefix + profileId);
}
