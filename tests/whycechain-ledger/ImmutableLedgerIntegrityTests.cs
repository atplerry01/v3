using Whycespace.System.Upstream.WhyceChain.Ledger;

namespace Whycespace.WhyceChain.Ledger.Tests;

public class ImmutableLedgerIntegrityTests
{
    private static ChainLedgerEntry MakeEntry(
        long seq, string entryHash, string previousEntryHash, string? entryType = "Event") =>
        new(Guid.NewGuid(), entryType!, "agg-1", seq, "payload-hash", "meta-hash",
            previousEntryHash, entryHash, DateTimeOffset.UtcNow, "trace", "corr", 1);

    private static ChainLedgerEntry GenesisEntry(string entryHash = "genesis-hash") =>
        MakeEntry(0, entryHash, string.Empty, "Genesis");

    private static ChainLedgerEntry NextEntry(long seq, string entryHash, string previousEntryHash) =>
        MakeEntry(seq, entryHash, previousEntryHash);

    [Fact]
    public void Validate_EmptyLedger_ShouldBeValid()
    {
        var ledger = new ImmutableEventLedger(Guid.NewGuid(), "trace-001");

        var result = ImmutableLedgerValidator.Validate(ledger);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Validate_ValidSingleEntry_ShouldBeValid()
    {
        var ledger = new ImmutableEventLedger(Guid.NewGuid(), "trace-001");
        ledger.AppendEntry(GenesisEntry());

        var result = ImmutableLedgerValidator.Validate(ledger);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Validate_ValidChain_ShouldBeValid()
    {
        var ledger = new ImmutableEventLedger(Guid.NewGuid(), "trace-001");
        ledger.AppendEntry(GenesisEntry("h0"));
        ledger.AppendEntry(NextEntry(1, "h1", "h0"));
        ledger.AppendEntry(NextEntry(2, "h2", "h1"));
        ledger.AppendEntry(NextEntry(3, "h3", "h2"));

        var result = ImmutableLedgerValidator.Validate(ledger);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Validate_GenesisHashMismatch_ShouldReportIssue()
    {
        var ledger = new ImmutableEventLedger(Guid.NewGuid(), "trace-001");
        ledger.AppendEntry(GenesisEntry("h0"));

        // Tamper with GenesisHash via reflection to simulate corruption
        var prop = typeof(ImmutableEventLedger).GetProperty(nameof(ImmutableEventLedger.GenesisHash))!;
        prop.SetValue(ledger, "tampered");

        var result = ImmutableLedgerValidator.Validate(ledger);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("Genesis hash mismatch"));
    }

    [Fact]
    public void Validate_GenesisEntryCorrectness()
    {
        var ledger = new ImmutableEventLedger(Guid.NewGuid(), "trace-001");
        ledger.AppendEntry(GenesisEntry("genesis-value"));

        var result = ImmutableLedgerValidator.Validate(ledger);

        Assert.True(result.IsValid);
        Assert.Equal("genesis-value", ledger.GenesisHash);
        Assert.Equal("genesis-value", ledger.Entries[0].EntryHash);
    }

    [Fact]
    public void Validate_SequenceContinuity_IsEnforced()
    {
        var ledger = new ImmutableEventLedger(Guid.NewGuid(), "trace-001");
        ledger.AppendEntry(GenesisEntry("h0"));
        ledger.AppendEntry(NextEntry(1, "h1", "h0"));

        var result = ImmutableLedgerValidator.Validate(ledger);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_HashChainIntegrity_IsChecked()
    {
        var ledger = new ImmutableEventLedger(Guid.NewGuid(), "trace-001");
        ledger.AppendEntry(GenesisEntry("h0"));
        ledger.AppendEntry(NextEntry(1, "h1", "h0"));
        ledger.AppendEntry(NextEntry(2, "h2", "h1"));

        var result = ImmutableLedgerValidator.Validate(ledger);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }
}
