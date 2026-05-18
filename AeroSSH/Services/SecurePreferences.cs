using Android.Content;
using AndroidX.Security.Crypto;

namespace AeroSSH.Services;

public class SecurePreferences : IKeyValueStore
{
    private const string PrefsName = "aerossh_secure";
    private readonly ISharedPreferences _prefs;

    public SecurePreferences(Context context)
    {
        var masterKey = new MasterKey.Builder(context)
            .SetKeyScheme(MasterKey.KeyScheme.Aes256Gcm)
            .Build();

        _prefs = EncryptedSharedPreferences.Create(
            context,
            PrefsName,
            masterKey,
            EncryptedSharedPreferences.PrefKeyEncryptionScheme.Aes256Siv,
            EncryptedSharedPreferences.PrefValueEncryptionScheme.Aes256Gcm)!;
    }

    public string? GetString(string key) => _prefs.GetString(key, null);

    public void PutString(string key, string value)
    {
        var editor = _prefs.Edit()!;
        editor.PutString(key, value);
        editor.Apply();
    }

    public void Remove(string key)
    {
        var editor = _prefs.Edit()!;
        editor.Remove(key);
        editor.Apply();
    }

    public IEnumerable<string> KeysWithPrefix(string prefix)
    {
        var all = _prefs.All;
        if (all == null) yield break;
        foreach (var key in all.Keys)
            if (key.StartsWith(prefix, StringComparison.Ordinal))
                yield return key;
    }
}
