using Whycespace.Engines.T0U.WhyceChain.Block.Builder;
using Whycespace.Engines.T0U.WhyceChain.Block.Anchor;
using Whycespace.Engines.T0U.WhyceChain.Ledger.Event;
using Whycespace.Engines.T0U.WhyceChain.Ledger.Immutable;
using Whycespace.Engines.T0U.WhyceChain.Ledger.Indexing;
using Whycespace.Engines.T0U.WhyceChain.Verification.Integrity;
using Whycespace.Engines.T0U.WhyceChain.Verification.Merkle;
using Whycespace.Engines.T0U.WhyceChain.Verification.Audit;
using Whycespace.Engines.T0U.WhyceChain.Replication.Replication;
using Whycespace.Engines.T0U.WhyceChain.Replication.Snapshot;
using Whycespace.Engines.T0U.WhyceChain.Append.Execution;
using Whycespace.Engines.T0U.WhyceChain.Evidence.Hashing;
using Whycespace.Engines.T0U.WhyceChain.Evidence.Anchoring;
using Whycespace.Engines.T0U.WhyceChain.Evidence.Gateway;
using Whycespace.Systems.Upstream.WhyceChain.Ledger;

namespace Whycespace.WhyceChain.Chain.Tests;

public class ChainAppendEngineTests
{
    private readonly ChainAppendEngine _engine = new();

    private static ChainLedgerEntry CreateEntry(string payloadHash, DateTimeOffset timestamp, long sequenceNumber = 0)
    {
        return new ChainLedgerEntry(
            EntryId: Guid.NewGuid(),
            EntryType: "TestEvent",
            AggregateId: "test-aggregate",
            SequenceNumber: sequenceNumber,
            PayloadHash: payloadHash,
            MetadataHash: "meta-hash",
            PreviousEntryHash: "",
            EntryHash: ChainHashUtility.ComputeEntryHash(Guid.NewGuid().ToString(), payloadHash, ""),
            Timestamp: timestamp,
            TraceId: "trace-001",
            CorrelationId: "corr-001",
            EventVersion: 1);
    }

    private static ChainBlock CreateValidBlock(
        long blockHeight = 0,
        string? previousBlockHash = null,
        DateTime? createdAt = null,
        List<ChainLedgerEntry>? entries = null)
    {
        var now = createdAt ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        entries ??=
        [
            CreateEntry("hash1", baseTime, 0),
            CreateEntry("hash2", baseTime.AddSeconds(1), 1)
        ];

        var entryHashes = entries.Select(e => e.PayloadHash).ToList();
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

    private static ChainAppendCommand CreateCommand(
        ChainBlock block,
        long currentChainHeight = -1,
        string latestBlockHash = "")
    {
        return new ChainAppendCommand(
            currentChainHeight,
            latestBlockHash,
            block,
            "trace-001",
            "corr-001",
            DateTime.UtcNow);
    }

    // --- Valid append tests ---

    [Fact]
    public void Execute_ValidGenesisBlock_ShouldAccept()
    {
        var block = CreateValidBlock();
        var command = CreateCommand(block);

        var result = _engine.Execute(command);

        Assert.True(result.BlockAccepted);
        Assert.Equal(0, result.NewChainHeight);
        Assert.Equal(block.BlockHash, result.AppendedBlockHash);
        Assert.True(result.ChainContinuityValid);
        Assert.Empty(result.ValidationErrors);
        Assert.Equal("trace-001", result.TraceId);
    }

    [Fact]
    public void Execute_ValidSequentialBlock_ShouldAccept()
    {
        var genesis = CreateValidBlock();
        var block1 = CreateValidBlock(
            blockHeight: 1,
            previousBlockHash: genesis.BlockHash,
            createdAt: new DateTime(2026, 1, 1, 1, 0, 0, DateTimeKind.Utc));

        var command = CreateCommand(block1, currentChainHeight: 0, latestBlockHash: genesis.BlockHash);
        var result = _engine.Execute(command);

        Assert.True(result.BlockAccepted);
        Assert.Equal(1, result.NewChainHeight);
        Assert.Equal(block1.BlockHash, result.AppendedBlockHash);
        Assert.True(result.ChainContinuityValid);
    }

    // --- Invalid height tests ---

    [Fact]
    public void Execute_IncorrectBlockHeight_ShouldReject()
    {
        var block = CreateValidBlock(blockHeight: 5);
        var command = CreateCommand(block, currentChainHeight: 0, latestBlockHash: "some-hash");

        var result = _engine.Execute(command);

        Assert.False(result.BlockAccepted);
        Assert.False(result.ChainContinuityValid);
        Assert.Contains(result.ValidationErrors, e => e.Contains("BlockHeight mismatch"));
        Assert.Equal(0, result.NewChainHeight);
    }

    [Fact]
    public void Execute_DuplicateBlockHeight_ShouldReject()
    {
        var block = CreateValidBlock(blockHeight: 0);
        var command = CreateCommand(block, currentChainHeight: 0, latestBlockHash: "some-hash");

        var result = _engine.Execute(command);

        Assert.False(result.BlockAccepted);
        Assert.Contains(result.ValidationErrors, e => e.Contains("BlockHeight mismatch"));
    }

    // --- Invalid previous hash tests ---

    [Fact]
    public void Execute_IncorrectPreviousHash_ShouldReject()
    {
        var block = CreateValidBlock(blockHeight: 1, previousBlockHash: "wrong-hash");
        var command = CreateCommand(block, currentChainHeight: 0, latestBlockHash: "correct-hash");

        var result = _engine.Execute(command);

        Assert.False(result.BlockAccepted);
        Assert.False(result.ChainContinuityValid);
        Assert.Contains(result.ValidationErrors, e => e.Contains("PreviousBlockHash mismatch"));
    }

    [Fact]
    public void Execute_GenesisBlockWithNonNullPreviousHash_ShouldReject()
    {
        var block = CreateValidBlock(blockHeight: 0, previousBlockHash: "should-be-null");
        var command = CreateCommand(block, currentChainHeight: -1);

        var result = _engine.Execute(command);

        Assert.False(result.BlockAccepted);
        Assert.Contains(result.ValidationErrors, e => e.Contains("Genesis block must have null PreviousBlockHash"));
    }

    // --- Tampered block tests ---

    [Fact]
    public void Execute_TamperedBlockHash_ShouldReject()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var entries = new List<ChainLedgerEntry>
        {
            CreateEntry("hash1", baseTime)
        };
        var entryHashes = entries.Select(e => e.PayloadHash).ToList();
        var merkleRoot = ChainHashUtility.ComputeMerkleRoot(entryHashes);

        var block = new ChainBlock(
            Guid.NewGuid(), 0, null, entries, merkleRoot,
            "tampered-block-hash", entries.Count, now, "sig", "trace-001");

        var command = CreateCommand(block);
        var result = _engine.Execute(command);

        Assert.False(result.BlockAccepted);
        Assert.Contains(result.ValidationErrors, e => e.Contains("BlockHash is not deterministic"));
    }

    [Fact]
    public void Execute_TamperedMerkleRoot_ShouldReject()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var entries = new List<ChainLedgerEntry>
        {
            CreateEntry("hash1", baseTime)
        };
        var wrongMerkle = "tampered-merkle-root";
        var blockHash = ChainHashUtility.ComputeBlockHash(0, null, wrongMerkle, entries.Count, now);

        var block = new ChainBlock(
            Guid.NewGuid(), 0, null, entries, wrongMerkle,
            blockHash, entries.Count, now, "sig", "trace-001");

        var command = CreateCommand(block);
        var result = _engine.Execute(command);

        Assert.False(result.BlockAccepted);
        Assert.Contains(result.ValidationErrors, e => e.Contains("MerkleRoot does not match"));
    }

    [Fact]
    public void Execute_MismatchedEntryCount_ShouldReject()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var entries = new List<ChainLedgerEntry>
        {
            CreateEntry("hash1", baseTime)
        };
        var entryHashes = entries.Select(e => e.PayloadHash).ToList();
        var merkleRoot = ChainHashUtility.ComputeMerkleRoot(entryHashes);
        var wrongCount = 5;
        var blockHash = ChainHashUtility.ComputeBlockHash(0, null, merkleRoot, wrongCount, now);

        var block = new ChainBlock(
            Guid.NewGuid(), 0, null, entries, merkleRoot,
            blockHash, wrongCount, now, "sig", "trace-001");

        var command = CreateCommand(block);
        var result = _engine.Execute(command);

        Assert.False(result.BlockAccepted);
        Assert.Contains(result.ValidationErrors, e => e.Contains("EntryCount"));
    }

    [Fact]
    public void Execute_EmptyEntries_ShouldReject()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var entries = new List<ChainLedgerEntry>();
        var merkleRoot = ChainHashUtility.ComputeMerkleRoot(entries.Select(e => e.PayloadHash).ToList());
        var blockHash = ChainHashUtility.ComputeBlockHash(0, null, merkleRoot, 0, now);

        var block = new ChainBlock(
            Guid.NewGuid(), 0, null, entries, merkleRoot,
            blockHash, 0, now, "sig", "trace-001");

        var command = CreateCommand(block);
        var result = _engine.Execute(command);

        Assert.False(result.BlockAccepted);
        Assert.Contains(result.ValidationErrors, e => e.Contains("at least one entry"));
    }

    // --- Determinism test ---

    [Fact]
    public void Execute_IsDeterministic_SameInputSameResult()
    {
        var block = CreateValidBlock();
        var command = CreateCommand(block);

        var result1 = _engine.Execute(command);
        var result2 = _engine.Execute(command);

        Assert.Equal(result1.BlockAccepted, result2.BlockAccepted);
        Assert.Equal(result1.NewChainHeight, result2.NewChainHeight);
        Assert.Equal(result1.AppendedBlockHash, result2.AppendedBlockHash);
        Assert.Equal(result1.ChainContinuityValid, result2.ChainContinuityValid);
        Assert.Equal(result1.ValidationErrors.Count, result2.ValidationErrors.Count);
    }

    // --- Entry ordering test ---

    [Fact]
    public void Execute_UnorderedEntries_ShouldReject()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var entries = new List<ChainLedgerEntry>
        {
            CreateEntry("hash1", baseTime.AddSeconds(10), 0),
            CreateEntry("hash2", baseTime, 1)
        };
        var entryHashes = entries.Select(e => e.PayloadHash).ToList();
        var merkleRoot = ChainHashUtility.ComputeMerkleRoot(entryHashes);
        var blockHash = ChainHashUtility.ComputeBlockHash(0, null, merkleRoot, entries.Count, now);

        var block = new ChainBlock(
            Guid.NewGuid(), 0, null, entries, merkleRoot,
            blockHash, entries.Count, now, "sig", "trace-001");

        var command = CreateCommand(block);
        var result = _engine.Execute(command);

        Assert.False(result.BlockAccepted);
        Assert.Contains(result.ValidationErrors, e => e.Contains("Entries are not ordered"));
    }
}
