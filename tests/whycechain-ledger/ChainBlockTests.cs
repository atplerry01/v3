using Whycespace.System.Upstream.WhyceChain.Ledger;
using ChainBlock = Whycespace.System.Upstream.WhyceChain.Ledger.ChainBlock;
using ChainLedgerEntry = Whycespace.System.Upstream.WhyceChain.Ledger.ChainLedgerEntry;

namespace Whycespace.WhyceChain.Ledger.Tests;

public class ChainBlockTests
{
    private static ChainLedgerEntry CreateEntry(
        long sequenceNumber,
        string payloadHash,
        string previousEntryHash,
        DateTimeOffset timestamp)
    {
        var entryId = Guid.NewGuid();
        var entryHash = ChainHashUtility.GenerateEntryHash(
            entryId, "TestEvent", "agg-1", sequenceNumber,
            payloadHash, "meta-hash", previousEntryHash,
            timestamp, "trace", "corr", 1);

        return new ChainLedgerEntry(
            entryId, "TestEvent", "agg-1", sequenceNumber,
            payloadHash, "meta-hash", previousEntryHash,
            entryHash, timestamp, "trace", "corr", 1);
    }

    private static ChainBlock CreateValidBlock(
        long blockHeight = 0,
        string? previousBlockHash = null,
        DateTime? createdAt = null)
    {
        var now = createdAt ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var entries = new List<ChainLedgerEntry>
        {
            CreateEntry(0, "hash1", "", baseTime),
            CreateEntry(1, "hash2", "hash1", baseTime.AddSeconds(1))
        };

        var entryHashes = entries.Select(e => e.EntryHash).ToList();
        var merkleRoot = ChainHashUtility.ComputeMerkleRoot(entryHashes);
        var blockHash = ChainHashUtility.ComputeBlockHash(blockHeight, previousBlockHash, merkleRoot, entries.Count, now);

        return new ChainBlock(
            Guid.NewGuid(),
            blockHeight,
            previousBlockHash,
            entries,
            merkleRoot,
            blockHash,
            entries.Count,
            now,
            "validator-sig",
            "trace-001");
    }

    [Fact]
    public void BlockHash_IsDeterministic()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var hash1 = ChainHashUtility.ComputeBlockHash(0, null, "root1", 1, now);
        var hash2 = ChainHashUtility.ComputeBlockHash(0, null, "root1", 1, now);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void BlockHash_ChangesWithDifferentInputs()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var hash1 = ChainHashUtility.ComputeBlockHash(0, null, "root1", 1, now);
        var hash2 = ChainHashUtility.ComputeBlockHash(1, null, "root1", 1, now);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Block_EntryCount_MatchesEntries()
    {
        var block = CreateValidBlock();

        Assert.Equal(block.Entries.Count, block.EntryCount);
    }

    [Fact]
    public void Block_MerkleRoot_MatchesEntryHashes()
    {
        var block = CreateValidBlock();
        var expectedRoot = ChainHashUtility.ComputeMerkleRoot(
            block.Entries.Select(e => e.EntryHash).ToList());

        Assert.Equal(expectedRoot, block.MerkleRoot);
    }

    [Fact]
    public void Block_IsImmutable()
    {
        var block = CreateValidBlock();

        Assert.IsAssignableFrom<IReadOnlyList<ChainLedgerEntry>>(block.Entries);

        var modified = block with { BlockHeight = 99 };
        Assert.NotEqual(block.BlockHeight, modified.BlockHeight);
        Assert.Equal(0, block.BlockHeight);
    }

    [Fact]
    public void GenesisBlock_HasNullPreviousBlockHash()
    {
        var block = CreateValidBlock(blockHeight: 0, previousBlockHash: null);

        Assert.Null(block.PreviousBlockHash);
        Assert.Equal(0, block.BlockHeight);
    }

    [Fact]
    public void Block_PreservesTraceAndValidatorSignature()
    {
        var block = CreateValidBlock();

        Assert.Equal("validator-sig", block.ValidatorSignature);
        Assert.Equal("trace-001", block.TraceId);
    }
}
