using AeroSSH.Models;
using AeroSSH.Services;
using Xunit;

namespace AeroSSH.Tests;

public class ProfileStoreTests
{
    [Fact]
    public void Save_And_Get_RoundTrip_Preserves_Fields()
    {
        var store = new ProfileStore(new InMemoryKeyValueStore());
        var profile = new ServerProfile
        {
            Name = "Prod",
            Host = "example.com",
            Port = 2222,
            Username = "deploy",
            AuthMethod = AuthMethod.PrivateKey,
            PrivateKey = new byte[] { 1, 2, 3 },
            KeyPassphrase = "secret"
        };

        store.Save(profile);
        var loaded = store.Get(profile.Id);

        Assert.NotNull(loaded);
        Assert.Equal(profile.Id, loaded!.Id);
        Assert.Equal("Prod", loaded.Name);
        Assert.Equal("example.com", loaded.Host);
        Assert.Equal(2222, loaded.Port);
        Assert.Equal("deploy", loaded.Username);
        Assert.Equal(AuthMethod.PrivateKey, loaded.AuthMethod);
        Assert.Equal(new byte[] { 1, 2, 3 }, loaded.PrivateKey);
        Assert.Equal("secret", loaded.KeyPassphrase);
    }

    [Fact]
    public void GetAll_Returns_Sorted_By_LastUsed_Desc()
    {
        var store = new ProfileStore(new InMemoryKeyValueStore());
        var older = new ServerProfile { Name = "old", Host = "h", Username = "u", LastUsedAt = DateTime.UtcNow.AddDays(-1) };
        var newer = new ServerProfile { Name = "new", Host = "h", Username = "u", LastUsedAt = DateTime.UtcNow };
        store.Save(older);
        store.Save(newer);

        var all = store.GetAll();
        Assert.Equal(2, all.Count);
        Assert.Equal("new", all[0].Name);
        Assert.Equal("old", all[1].Name);
    }

    [Fact]
    public void Delete_Removes_Profile()
    {
        var store = new ProfileStore(new InMemoryKeyValueStore());
        var p = new ServerProfile { Name = "t", Host = "h", Username = "u" };
        store.Save(p);
        store.Delete(p.Id);
        Assert.Null(store.Get(p.Id));
        Assert.Empty(store.GetAll());
    }

    [Fact]
    public void TouchLastUsed_Updates_Timestamp()
    {
        var store = new ProfileStore(new InMemoryKeyValueStore());
        var p = new ServerProfile { Name = "t", Host = "h", Username = "u", LastUsedAt = DateTime.UtcNow.AddDays(-7) };
        store.Save(p);
        store.TouchLastUsed(p.Id);
        var loaded = store.Get(p.Id)!;
        Assert.True(loaded.LastUsedAt > DateTime.UtcNow.AddMinutes(-1));
    }
}
