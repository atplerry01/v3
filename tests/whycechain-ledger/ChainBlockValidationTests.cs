using Whycespace.System.Upstream.WhyceChain.Ledger;
using ChainBlock = Whycespace.System.Upstream.WhyceChain.Ledger.ChainBlock;
using ChainLedgerEntry = Whycespace.System.Upstream.WhyceChain.Ledger.ChainLedgerEntry;

namespace Whycespace.WhyceChain.Ledger.Tests;

public class ChainBlockValidationTests
{
    private readonly ChainBlockValidator _validator = new();

    private static ChainLedgerEntry CreateEntry(long seq, string payloadHash, string previousEntryHash, DateTimeOffset timestamp)
    {
        var entryId = Guid.NewGuid();
        var entryHash = ChainHashUtility.GenerateEntryHash(
            entryId, "TestEvent", "agg-1", seq, payloadHash, "meta-hash",
            previousEntryHash, timestamp, "trace", "corr", 1);
        return new ChainLedgerEntry(
            entryId, "TestEvent", "agg-1", seq, payloadHash, "meta-hash",
            previousEntryHash, entryHash, timestamp, "trace", "corr", 1);
    }

    private static ChainBlock CreateValidBlock(
        long blockHeight = 0,
        string? previousBlockHash = null,
        List<ChainLedgerEntry>? entries = null,
        DateTime? createdAt = null)
    {
        var now = createdAt ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        entries ??= new List<ChainLedgerEntry>
        {
            CreateEntry(0, "hash1", "", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            CreateEntry(1, "hash2", "hash1", new DateTimeOffset(2026, 1, 1, 0, 0, 1, TimeSpan.Zero))
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
    public void Validate_ValidGenesisBlock_ReturnsValid()
    {
        var block = CreateValidBlock();

        var result = _validator.Validate(block);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_EmptyEntries_ReturnsInvalid()
    {
        var block = CreateValidBlock() with
        {
            Entries = Array.Empty<ChainLedgerEntry>(),
            EntryCount = 0
        };

        var result = _validator.Validate(block);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("at least one entry"));
    }

    [Fact]
    public void Validate_UnorderedEntries_ReturnsInvalid()
    {
        var entries = new List<ChainLedgerEntry>
        {
            CreateEntry(1, "hash1", "", new DateTimeOffset(2026, 1, 1, 0, 0, 5, TimeSpan.Zero)),
            CreateEntry(0, "hash2", "hash1", new DateTimeOffset(2026, 1, 1, 0, 0, 1, TimeSpan.Zero))
        };

        var block = CreateValidBlock(entries: entries);

        var result = _validator.Validate(block);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not ordered"));
    }

    [Fact]
    public void Validate_WrongEntryCount_ReturnsInvalid()
    {
        var block = CreateValidBlock() with { EntryCount = 99 };

        var result = _validator.Validate(block);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("EntryCount"));
    }

    [Fact]
    public void Validate_WrongMerkleRoot_ReturnsInvalid()
    {
        var block = CreateValidBlock() with { MerkleRoot = "wrong-root" };

        var result = _validator.Validate(block);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("MerkleRoot"));
    }

    [Fact]
    public void Validate_WrongBlockHash_ReturnsInvalid()
    {
        var block = CreateValidBlock() with { BlockHash = "tampered-hash" };

        var result = _validator.Validate(block);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("BlockHash"));
    }

    [Fact]
    public void Validate_GenesisBlockWithPreviousHash_ReturnsInvalid()
    {
        var block = CreateValidBlock(blockHeight: 0, previousBlockHash: null);
        // Re-create with a non-null PreviousBlockHash but height 0
        var now = block.CreatedAt;
        var badHash = ChainHashUtility.ComputeBlockHash(0, "should-be-null", block.MerkleRoot, block.EntryCount, now);
        var badBlock = block with { PreviousBlockHash = "should-be-null", BlockHash = badHash };

        var result = _validator.Validate(badBlock);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Genesis block"));
    }

    [Fact]
    public void Validate_NonGenesisBlockWithoutPreviousHash_ReturnsInvalid()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var entries = new List<ChainLedgerEntry>
        {
            CreateEntry(0, "hash1", "", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))
        };
        var entryHashes = entries.Select(e => e.EntryHash).ToList();
        var merkleRoot = ChainHashUtility.ComputeMerkleRoot(entryHashes);
        var blockHash = ChainHashUtility.ComputeBlockHash(1, null, merkleRoot, 1, now);

        var block = new ChainBlock(
            Guid.NewGuid(), 1, null, entries, merkleRoot, blockHash, 1, now, "sig", "trace");

        var result = _validator.Validate(block);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("PreviousBlockHash"));
    }

    [Fact]
    public void Validate_ChainedBlock_WithCorrectPreviousHash_ReturnsValid()
    {
        var genesis = CreateValidBlock();
        var now = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var entries = new List<ChainLedgerEntry>
        {
            CreateEntry(0, "hash3", "hash2", new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero))
        };
        var entryHashes = entries.Select(e => e.EntryHash).ToList();
        var merkleRoot = ChainHashUtility.ComputeMerkleRoot(entryHashes);
        var blockHash = ChainHashUtility.ComputeBlockHash(1, genesis.BlockHash, merkleRoot, 1, now);

        var block = new ChainBlock(
            Guid.NewGuid(), 1, genesis.BlockHash, entries, merkleRoot, blockHash, 1, now, "sig", "trace");

        var result = _validator.Validate(block, genesis);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ChainedBlock_WithWrongPreviousHash_ReturnsInvalid()
    {
        var genesis = CreateValidBlock();
        var now = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var entries = new List<ChainLedgerEntry>
        {
            CreateEntry(0, "hash3", "hash2", new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero))
        };
        var entryHashes = entries.Select(e => e.EntryHash).ToList();
        var merkleRoot = ChainHashUtility.ComputeMerkleRoot(entryHashes);
        var blockHash = ChainHashUtility.ComputeBlockHash(1, "wrong-prev", merkleRoot, 1, now);

        var block = new ChainBlock(
            Guid.NewGuid(), 1, "wrong-prev", entries, merkleRoot, blockHash, 1, now, "sig", "trace");

        var result = _validator.Validate(block, genesis);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("PreviousBlockHash"));
    }
}
