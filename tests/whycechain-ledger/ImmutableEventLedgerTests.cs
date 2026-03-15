using Whycespace.System.Upstream.WhyceChain.Ledger;

namespace Whycespace.WhyceChain.Ledger.Tests;

public class ImmutableEventLedgerTests
{
    private readonly ImmutableEventLedger _ledger;

    public ImmutableEventLedgerTests()
    {
        _ledger = new ImmutableEventLedger(Guid.NewGuid(), "trace-001");
    }

    private static ChainLedgerEntry MakeEntry(
        long seq, string entryHash, string previousEntryHash, string? entryType = "Event") =>
        new(Guid.NewGuid(), entryType!, "agg-1", seq, "payload-hash", "meta-hash",
            previousEntryHash, entryHash, DateTimeOffset.UtcNow, "trace", "corr", 1);

    private static ChainLedgerEntry GenesisEntry(string entryHash = "genesis-hash") =>
        MakeEntry(0, entryHash, string.Empty, "Genesis");

    private static ChainLedgerEntry NextEntry(long seq, string entryHash, string previousEntryHash) =>
        MakeEntry(seq, entryHash, previousEntryHash);

    [Fact]
    public void AppendEntry_GenesisEntry_ShouldSucceed()
    {
        var genesis = GenesisEntry();

        _ledger.AppendEntry(genesis);

        Assert.Equal(1, _ledger.CurrentHeight);
        Assert.Equal("genesis-hash", _ledger.GenesisHash);
        Assert.Equal("genesis-hash", _ledger.LatestEntryHash);
    }

    [Fact]
    public void AppendEntry_GenesisWithNonEmptyPreviousHash_ShouldThrow()
    {
        var bad = MakeEntry(0, "hash", "non-empty", "Genesis");

        Assert.Throws<InvalidOperationException>(() => _ledger.AppendEntry(bad));
    }

    [Fact]
    public void AppendEntry_SecondEntry_ShouldChainHash()
    {
        _ledger.AppendEntry(GenesisEntry("h0"));

        _ledger.AppendEntry(NextEntry(1, "h1", "h0"));

        Assert.Equal(2, _ledger.CurrentHeight);
        Assert.Equal("h1", _ledger.LatestEntryHash);
    }

    [Fact]
    public void AppendEntry_WrongPreviousHash_ShouldThrow()
    {
        _ledger.AppendEntry(GenesisEntry());

        var bad = NextEntry(1, "h1", "wrong-hash");

        Assert.Throws<InvalidOperationException>(() => _ledger.AppendEntry(bad));
    }

    [Fact]
    public void AppendEntry_WrongSequence_ShouldThrow()
    {
        _ledger.AppendEntry(GenesisEntry("h0"));

        var bad = NextEntry(5, "h1", "h0");

        Assert.Throws<InvalidOperationException>(() => _ledger.AppendEntry(bad));
    }

    [Fact]
    public void AppendEntry_EmptyEntryHash_ShouldThrow()
    {
        _ledger.AppendEntry(GenesisEntry("h0"));

        var bad = MakeEntry(1, "", "h0");

        Assert.Throws<InvalidOperationException>(() => _ledger.AppendEntry(bad));
    }

    [Fact]
    public void AppendOnly_EntriesListIsImmutableExternally()
    {
        _ledger.AppendEntry(GenesisEntry());

        var entries = _ledger.Entries;

        Assert.IsAssignableFrom<IReadOnlyList<ChainLedgerEntry>>(entries);
    }

    [Fact]
    public void AppendEntry_MultipleEntries_ShouldMaintainOrder()
    {
        _ledger.AppendEntry(GenesisEntry("h0"));
        _ledger.AppendEntry(NextEntry(1, "h1", "h0"));
        _ledger.AppendEntry(NextEntry(2, "h2", "h1"));

        Assert.Equal(3, _ledger.CurrentHeight);
        Assert.Equal("h0", _ledger.Entries[0].EntryHash);
        Assert.Equal("h1", _ledger.Entries[1].EntryHash);
        Assert.Equal("h2", _ledger.Entries[2].EntryHash);
    }

    [Fact]
    public void GetEntry_ValidSequence_ShouldReturnCorrectEntry()
    {
        _ledger.AppendEntry(GenesisEntry("h0"));
        var e1 = NextEntry(1, "h1", "h0");
        _ledger.AppendEntry(e1);

        var entry = _ledger.GetEntry(1);

        Assert.Equal(e1.EntryId, entry.EntryId);
    }

    [Fact]
    public void GetEntry_OutOfRange_ShouldThrow()
    {
        _ledger.AppendEntry(GenesisEntry());

        Assert.Throws<ArgumentOutOfRangeException>(() => _ledger.GetEntry(5));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ledger.GetEntry(-1));
    }

    [Fact]
    public void GetLatestEntry_ShouldReturnLastEntry()
    {
        _ledger.AppendEntry(GenesisEntry("h0"));
        _ledger.AppendEntry(NextEntry(1, "h1", "h0"));
        var e2 = NextEntry(2, "h2", "h1");
        _ledger.AppendEntry(e2);

        var latest = _ledger.GetLatestEntry();

        Assert.Equal(e2.EntryId, latest.EntryId);
        Assert.Equal("h2", latest.EntryHash);
    }

    [Fact]
    public void GetLatestEntry_EmptyLedger_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() => _ledger.GetLatestEntry());
    }

    [Fact]
    public void GetEntriesRange_ShouldReturnCorrectSubset()
    {
        _ledger.AppendEntry(GenesisEntry("h0"));
        var e1 = NextEntry(1, "h1", "h0");
        var e2 = NextEntry(2, "h2", "h1");
        _ledger.AppendEntry(e1);
        _ledger.AppendEntry(e2);
        _ledger.AppendEntry(NextEntry(3, "h3", "h2"));

        var range = _ledger.GetEntriesRange(1, 2);

        Assert.Equal(2, range.Count);
        Assert.Equal(e1.EntryId, range[0].EntryId);
        Assert.Equal(e2.EntryId, range[1].EntryId);
    }

    [Fact]
    public void GetEntriesRange_InvalidRange_ShouldThrow()
    {
        _ledger.AppendEntry(GenesisEntry());

        Assert.Throws<ArgumentOutOfRangeException>(() => _ledger.GetEntriesRange(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ledger.GetEntriesRange(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ledger.GetEntriesRange(0, 5));
    }

    [Fact]
    public void SequenceIncrements_ShouldBeCorrect()
    {
        _ledger.AppendEntry(GenesisEntry("h0"));
        Assert.Equal(1, _ledger.CurrentHeight);

        _ledger.AppendEntry(NextEntry(1, "h1", "h0"));
        Assert.Equal(2, _ledger.CurrentHeight);

        _ledger.AppendEntry(NextEntry(2, "h2", "h1"));
        Assert.Equal(3, _ledger.CurrentHeight);
    }

    [Fact]
    public void GenesisHash_ShouldMatchFirstEntryHash()
    {
        _ledger.AppendEntry(GenesisEntry("my-genesis"));

        Assert.Equal("my-genesis", _ledger.GenesisHash);
        Assert.Equal(_ledger.Entries[0].EntryHash, _ledger.GenesisHash);
    }

    [Fact]
    public void PreviousHashLinkage_ShouldFormChain()
    {
        _ledger.AppendEntry(GenesisEntry("h0"));
        _ledger.AppendEntry(NextEntry(1, "h1", "h0"));
        _ledger.AppendEntry(NextEntry(2, "h2", "h1"));

        for (var i = 1; i < _ledger.Entries.Count; i++)
        {
            Assert.Equal(_ledger.Entries[i - 1].EntryHash, _ledger.Entries[i].PreviousEntryHash);
        }
    }
}
