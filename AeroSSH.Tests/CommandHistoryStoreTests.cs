using AeroSSH.Services;
using Xunit;

namespace AeroSSH.Tests;

public class CommandHistoryStoreTests
{
    [Fact]
    public void Add_Inserts_At_Head_And_Deduplicates()
    {
        var store = new CommandHistoryStore(new InMemoryKeyValueStore());
        store.Add("p1", "ls");
        store.Add("p1", "pwd");
        store.Add("p1", "ls");

        var history = store.Get("p1");
        Assert.Equal(new[] { "ls", "pwd" }, history);
    }

    [Fact]
    public void Add_Ignores_Empty()
    {
        var store = new CommandHistoryStore(new InMemoryKeyValueStore());
        store.Add("p1", "");
        store.Add("p1", "   ");
        Assert.Empty(store.Get("p1"));
    }

    [Fact]
    public void Histories_Are_Per_Profile()
    {
        var store = new CommandHistoryStore(new InMemoryKeyValueStore());
        store.Add("p1", "ls");
        store.Add("p2", "pwd");
        Assert.Equal(new[] { "ls" }, store.Get("p1"));
        Assert.Equal(new[] { "pwd" }, store.Get("p2"));
    }

    [Fact]
    public void Clear_Removes_History()
    {
        var store = new CommandHistoryStore(new InMemoryKeyValueStore());
        store.Add("p1", "ls");
        store.Clear("p1");
        Assert.Empty(store.Get("p1"));
    }

    [Fact]
    public void Trims_To_Max_Entries()
    {
        var store = new CommandHistoryStore(new InMemoryKeyValueStore());
        for (var i = 0; i < 150; i++) store.Add("p1", $"cmd{i}");
        Assert.Equal(100, store.Get("p1").Count);
        Assert.Equal("cmd149", store.Get("p1")[0]);
    }
}
