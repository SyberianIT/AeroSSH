using System.Security.Cryptography;

namespace AeroSSH.Services;

public class HostKeyStore
{
    private const string KeyPrefix = "hostkey_";
    private readonly IKeyValueStore _prefs;

    public HostKeyStore(IKeyValueStore prefs)
    {
        _prefs = prefs;
    }

    public string? GetFingerprint(string host, int port) =>
        _prefs.GetString(Key(host, port));

    public void Trust(string host, int port, string fingerprint) =>
        _prefs.PutString(Key(host, port), fingerprint);

    public void Forget(string host, int port) =>
        _prefs.Remove(Key(host, port));

    public IEnumerable<(string Host, int Port, string Fingerprint)> All()
    {
        foreach (var key in _prefs.KeysWithPrefix(KeyPrefix))
        {
            var fp = _prefs.GetString(key);
            if (fp == null) continue;
            var rest = key.Substring(KeyPrefix.Length);
            var colon = rest.LastIndexOf(':');
            if (colon <= 0 || !int.TryParse(rest.AsSpan(colon + 1), out var port)) continue;
            yield return (rest.Substring(0, colon), port, fp);
        }
    }

    public static string ComputeFingerprint(byte[] hostKey)
    {
        var hash = SHA256.HashData(hostKey);
        return "SHA256:" + Convert.ToBase64String(hash).TrimEnd('=');
    }

    private static string Key(string host, int port) => $"{KeyPrefix}{host}:{port}";
}
