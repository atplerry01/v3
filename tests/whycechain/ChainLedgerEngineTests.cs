using Whycespace.Engines.T0U.WhyceChain;
using Whycespace.System.Upstream.WhyceChain.Stores;

namespace Whycespace.WhyceChain.Tests;

public class ChainLedgerEngineTests
{
    private readonly ChainLedgerStore _store;
    private readonly ChainLedgerEngine _engine;

    public ChainLedgerEngineTests()
    {
        _store = new ChainLedgerStore();
        _engine = new ChainLedgerEngine(_store);
    }

    [Fact]
    public void RegisterEntry_ShouldStoreAndReturnEntry()
    {
        var entry = _engine.RegisterEntry("entry-1", "PolicyDecision", "hash-abc");

        Assert.Equal("entry-1", entry.EntryId);
        Assert.Equal("PolicyDecision", entry.EventType);
        Assert.Equal("hash-abc", entry.PayloadHash);
        Assert.Equal("genesis", entry.PreviousHash);
        Assert.Null(entry.BlockId);
    }

    [Fact]
    public void RegisterEntry_DuplicateId_ShouldThrow()
    {
        _engine.RegisterEntry("entry-1", "PolicyDecision", "hash-abc");

        Assert.Throws<InvalidOperationException>(() =>
            _engine.RegisterEntry("entry-1", "PolicyDecision", "hash-def"));
    }

    [Fact]
    public void GetEntry_ShouldReturnStoredEntry()
    {
        _engine.RegisterEntry("entry-1", "PolicyDecision", "hash-abc");

        var result = _engine.GetEntry("entry-1");

        Assert.Equal("entry-1", result.EntryId);
        Assert.Equal("PolicyDecision", result.EventType);
    }

    [Fact]
    public void ListEntries_ShouldReturnAllEntries()
    {
        _engine.RegisterEntry("entry-1", "PolicyDecision", "hash-abc");
        _engine.RegisterEntry("entry-2", "FinancialAction", "hash-def");
        _engine.RegisterEntry("entry-3", "GovernanceVote", "hash-ghi");

        var entries = _engine.ListEntries();

        Assert.Equal(3, entries.Count);
    }

    [Fact]
    public void RegisterEntry_ShouldChainPreviousHash()
    {
        var first = _engine.RegisterEntry("entry-1", "PolicyDecision", "hash-abc");
        var second = _engine.RegisterEntry("entry-2", "FinancialAction", "hash-def");
        var third = _engine.RegisterEntry("entry-3", "GovernanceVote", "hash-ghi");

        Assert.Equal("genesis", first.PreviousHash);
        Assert.Equal("hash-abc", second.PreviousHash);
        Assert.Equal("hash-def", third.PreviousHash);
    }
}
