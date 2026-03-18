using Whycespace.Systems.Upstream.WhyceChain.Ledger;
using LedgerEntry = Whycespace.Systems.Upstream.WhyceChain.Ledger.ChainLedgerEntry;

namespace Whycespace.WhyceChain.Ledger.Tests;

public class ChainLedgerEntryHashTests
{
    private static readonly Guid TestEntryId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset TestTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static LedgerEntry CreateEntry(
        long sequenceNumber = 0,
        string previousEntryHash = "genesis",
        string payloadHash = "payload-hash-1",
        string metadataHash = "metadata-hash-1")
    {
        var entryHash = ChainHashUtility.GenerateEntryHash(
            TestEntryId, "PolicyDecision", "agg-001", sequenceNumber,
            payloadHash, metadataHash, previousEntryHash,
            TestTimestamp, "trace-001", "corr-001", 1);

        return new LedgerEntry(
            TestEntryId, "PolicyDecision", "agg-001", sequenceNumber,
            payloadHash, metadataHash, previousEntryHash,
            entryHash, TestTimestamp, "trace-001", "corr-001", 1);
    }

    [Fact]
    public void GenerateEntryHash_IsDeterministic()
    {
        var hash1 = ChainHashUtility.GenerateEntryHash(
            TestEntryId, "PolicyDecision", "agg-001", 0,
            "payload", "meta", "prev",
            TestTimestamp, "trace", "corr", 1);

        var hash2 = ChainHashUtility.GenerateEntryHash(
            TestEntryId, "PolicyDecision", "agg-001", 0,
            "payload", "meta", "prev",
            TestTimestamp, "trace", "corr", 1);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GenerateEntryHash_ChangesWithDifferentPayloadHash()
    {
        var hash1 = ChainHashUtility.GenerateEntryHash(
            TestEntryId, "PolicyDecision", "agg-001", 0,
            "payload-A", "meta", "prev",
            TestTimestamp, "trace", "corr", 1);

        var hash2 = ChainHashUtility.GenerateEntryHash(
            TestEntryId, "PolicyDecision", "agg-001", 0,
            "payload-B", "meta", "prev",
            TestTimestamp, "trace", "corr", 1);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GenerateEntryHash_ChangesWithDifferentSequenceNumber()
    {
        var hash1 = ChainHashUtility.GenerateEntryHash(
            TestEntryId, "PolicyDecision", "agg-001", 0,
            "payload", "meta", "prev",
            TestTimestamp, "trace", "corr", 1);

        var hash2 = ChainHashUtility.GenerateEntryHash(
            TestEntryId, "PolicyDecision", "agg-001", 1,
            "payload", "meta", "prev",
            TestTimestamp, "trace", "corr", 1);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GenerateEntryHash_ChangesWithDifferentEventVersion()
    {
        var hash1 = ChainHashUtility.GenerateEntryHash(
            TestEntryId, "PolicyDecision", "agg-001", 0,
            "payload", "meta", "prev",
            TestTimestamp, "trace", "corr", 1);

        var hash2 = ChainHashUtility.GenerateEntryHash(
            TestEntryId, "PolicyDecision", "agg-001", 0,
            "payload", "meta", "prev",
            TestTimestamp, "trace", "corr", 2);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GenerateEntryHash_ProducesHexString()
    {
        var hash = ChainHashUtility.GenerateEntryHash(
            TestEntryId, "PolicyDecision", "agg-001", 0,
            "payload", "meta", "prev",
            TestTimestamp, "trace", "corr", 1);

        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    [Fact]
    public void Entry_IsImmutableRecord()
    {
        var entry = CreateEntry();
        var modified = entry with { SequenceNumber = 99 };

        Assert.Equal(0, entry.SequenceNumber);
        Assert.Equal(99, modified.SequenceNumber);
        Assert.NotEqual(entry, modified);
    }

    [Fact]
    public void Entry_ValidatorAcceptsCorrectHash()
    {
        var entry = CreateEntry();
        var result = ChainLedgerValidator.ValidateEntry(entry);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Entry_ValidatorRejectsTamperedHash()
    {
        var entry = CreateEntry() with { EntryHash = "tampered-hash" };
        var result = ChainLedgerValidator.ValidateEntry(entry);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("EntryHash mismatch"));
    }
}
