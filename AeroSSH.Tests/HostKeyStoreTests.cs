using AeroSSH.Services;
using Xunit;

namespace AeroSSH.Tests;

public class HostKeyStoreTests
{
    [Fact]
    public void Trust_And_Get_RoundTrip()
    {
        var store = new HostKeyStore(new InMemoryKeyValueStore());
        store.Trust("example.com", 22, "SHA256:abc");
        Assert.Equal("SHA256:abc", store.GetFingerprint("example.com", 22));
    }

    [Fact]
    public void Get_Unknown_Returns_Null()
    {
        var store = new HostKeyStore(new InMemoryKeyValueStore());
        Assert.Null(store.GetFingerprint("example.com", 22));
    }

    [Fact]
    public void Forget_Removes_Entry()
    {
        var store = new HostKeyStore(new InMemoryKeyValueStore());
        store.Trust("example.com", 22, "SHA256:abc");
        store.Forget("example.com", 22);
        Assert.Null(store.GetFingerprint("example.com", 22));
    }

    [Fact]
    public void All_Lists_Trusted_Hosts()
    {
        var store = new HostKeyStore(new InMemoryKeyValueStore());
        store.Trust("a.example", 22, "SHA256:a");
        store.Trust("b.example", 2222, "SHA256:b");
        var entries = store.All().ToList();
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.Host == "a.example" && e.Port == 22 && e.Fingerprint == "SHA256:a");
        Assert.Contains(entries, e => e.Host == "b.example" && e.Port == 2222 && e.Fingerprint == "SHA256:b");
    }

    [Fact]
    public void ComputeFingerprint_Is_Deterministic_And_Differentiates()
    {
        var a = HostKeyStore.ComputeFingerprint(new byte[] { 1, 2, 3 });
        var aAgain = HostKeyStore.ComputeFingerprint(new byte[] { 1, 2, 3 });
        var b = HostKeyStore.ComputeFingerprint(new byte[] { 1, 2, 4 });
        Assert.Equal(a, aAgain);
        Assert.NotEqual(a, b);
        Assert.StartsWith("SHA256:", a);
    }
}
