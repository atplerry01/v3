using Whycespace.Systems.Upstream.WhyceChain.Ledger;
using LedgerEntry = Whycespace.Systems.Upstream.WhyceChain.Ledger.ChainLedgerEntry;
using LedgerBlock = Whycespace.Systems.Upstream.WhyceChain.Ledger.ChainLedgerBlock;

namespace Whycespace.WhyceChain.Ledger.Tests;

public class ChainLedgerBlockTests
{
    private static readonly Guid TestBlockId = Guid.Parse("00000000-0000-0000-0000-000000000010");
    private static readonly DateTimeOffset TestCreatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static LedgerEntry CreateLedgerEntry(long seq, string previousEntryHash = "genesis")
    {
        var entryId = Guid.NewGuid();
        var timestamp = TestCreatedAt.AddSeconds(seq);
        var payloadHash = $"payload-{seq}";
        var metadataHash = $"meta-{seq}";

        var entryHash = ChainHashUtility.GenerateEntryHash(
            entryId, "TestEvent", "agg-001", seq,
            payloadHash, metadataHash, previousEntryHash,
            timestamp, "trace-001", "corr-001", 1);

        return new LedgerEntry(
            entryId, "TestEvent", "agg-001", seq,
            payloadHash, metadataHash, previousEntryHash,
            entryHash, timestamp, "trace-001", "corr-001", 1);
    }

    private static LedgerBlock CreateValidBlock(
        long blockHeight = 0,
        string previousBlockHash = "genesis",
        int entryCount = 2)
    {
        var entries = new List<LedgerEntry>();
        var prevHash = "genesis";
        for (var i = 0; i < entryCount; i++)
        {
            var entry = CreateLedgerEntry(i, prevHash);
            entries.Add(entry);
            prevHash = entry.EntryHash;
        }

        var entryHashes = entries.Select(e => e.EntryHash).ToList();
        var merkleRoot = ChainHashUtility.ComputeMerkleRoot(entryHashes);
        var blockHash = ChainHashUtility.GenerateBlockHash(
            TestBlockId, blockHeight, merkleRoot, previousBlockHash, entries.Count, TestCreatedAt);

        return new LedgerBlock(
            TestBlockId, blockHeight, entries, merkleRoot,
            previousBlockHash, blockHash, TestCreatedAt);
    }

    [Fact]
    public void BlockHash_IsDeterministic()
    {
        var block = CreateValidBlock();

        var recomputedHash = ChainHashUtility.GenerateBlockHash(
            block.BlockId, block.BlockHeight, block.MerkleRoot,
            block.PreviousBlockHash, block.Entries.Count, block.CreatedAt);

        Assert.Equal(block.BlockHash, recomputedHash);
    }

    [Fact]
    public void BlockHash_ChangesWithDifferentHeight()
    {
        var hash1 = ChainHashUtility.GenerateBlockHash(
            TestBlockId, 0, "root", "prev", 1, TestCreatedAt);
        var hash2 = ChainHashUtility.GenerateBlockHash(
            TestBlockId, 1, "root", "prev", 1, TestCreatedAt);

        Assert.NotEqual(hash1, hash2);
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
    public void Block_Validator_AcceptsValidBlock()
    {
        var block = CreateValidBlock();
        var result = ChainLedgerValidator.ValidateBlock(block);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Block_Validator_RejectsTamperedMerkleRoot()
    {
        var block = CreateValidBlock() with { MerkleRoot = "tampered-root" };
        var result = ChainLedgerValidator.ValidateBlock(block);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("MerkleRoot"));
    }

    [Fact]
    public void Block_Validator_RejectsTamperedBlockHash()
    {
        var block = CreateValidBlock() with { BlockHash = "tampered-hash" };
        var result = ChainLedgerValidator.ValidateBlock(block);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("BlockHash"));
    }

    [Fact]
    public void Block_Validator_RejectsEmptyEntries()
    {
        var block = CreateValidBlock() with { Entries = [] };
        var result = ChainLedgerValidator.ValidateBlock(block);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("at least one entry"));
    }

    [Fact]
    public void Block_IsImmutableRecord()
    {
        var block = CreateValidBlock();
        var modified = block with { BlockHeight = 99 };

        Assert.Equal(0, block.BlockHeight);
        Assert.Equal(99, modified.BlockHeight);
        Assert.NotEqual(block, modified);
    }

    [Fact]
    public void Block_EntriesAreReadOnly()
    {
        var block = CreateValidBlock();

        Assert.IsAssignableFrom<IReadOnlyList<LedgerEntry>>(block.Entries);
    }

    [Fact]
    public void Block_ChainValidation_AcceptsLinkedBlocks()
    {
        var block0 = CreateValidBlock(blockHeight: 0, previousBlockHash: "genesis");

        var block1Id = Guid.NewGuid();
        var entries1 = new List<LedgerEntry> { CreateLedgerEntry(2) };
        var merkle1 = ChainHashUtility.ComputeMerkleRoot(entries1.Select(e => e.EntryHash).ToList());
        var hash1 = ChainHashUtility.GenerateBlockHash(
            block1Id, 1, merkle1, block0.BlockHash, entries1.Count, TestCreatedAt);
        var block1 = new LedgerBlock(
            block1Id, 1, entries1, merkle1, block0.BlockHash, hash1, TestCreatedAt);

        var result = ChainLedgerValidator.ValidateBlock(block1, block0);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Block_ChainValidation_RejectsBrokenLink()
    {
        var block0 = CreateValidBlock(blockHeight: 0, previousBlockHash: "genesis");

        var block1Id = Guid.NewGuid();
        var entries1 = new List<LedgerEntry> { CreateLedgerEntry(2) };
        var merkle1 = ChainHashUtility.ComputeMerkleRoot(entries1.Select(e => e.EntryHash).ToList());
        var hash1 = ChainHashUtility.GenerateBlockHash(
            block1Id, 1, merkle1, "wrong-previous", entries1.Count, TestCreatedAt);
        var block1 = new LedgerBlock(
            block1Id, 1, entries1, merkle1, "wrong-previous", hash1, TestCreatedAt);

        var result = ChainLedgerValidator.ValidateBlock(block1, block0);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("PreviousBlockHash"));
    }

    [Fact]
    public void Block_SequenceValidation_AcceptsOrderedEntries()
    {
        var entries = new List<LedgerEntry>();
        var prevHash = "genesis";
        for (var i = 0; i < 5; i++)
        {
            var entry = CreateLedgerEntry(i, prevHash);
            entries.Add(entry);
            prevHash = entry.EntryHash;
        }

        var result = ChainLedgerValidator.ValidateEntrySequence(entries);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Block_SequenceValidation_RejectsGap()
    {
        var entry0 = CreateLedgerEntry(0);
        var entry2 = CreateLedgerEntry(2, entry0.EntryHash);

        var result = ChainLedgerValidator.ValidateEntrySequence([entry0, entry2]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("Sequence gap"));
    }
}
