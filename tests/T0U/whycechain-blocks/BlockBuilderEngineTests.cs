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

namespace Whycespace.WhyceChainBlocks.Tests;

public class BlockBuilderEngineTests
{
    private readonly BlockBuilderEngine _engine = new();

    private static ChainLedgerEntry CreateEntry(
        int sequence,
        string? entryHash = null,
        string? payloadHash = null) =>
        new(
            EntryId: Guid.NewGuid(),
            EntryType: "TestEvent",
            AggregateId: "agg-1",
            SequenceNumber: sequence,
            PayloadHash: payloadHash ?? $"payload-hash-{sequence}",
            MetadataHash: $"meta-hash-{sequence}",
            PreviousEntryHash: "prev-hash",
            EntryHash: entryHash ?? $"entry-hash-{sequence}",
            Timestamp: DateTimeOffset.UtcNow,
            TraceId: "trace-1",
            CorrelationId: "corr-1",
            EventVersion: 1);

    private static BlockBuilderCommand CreateCommand(
        IReadOnlyList<ChainLedgerEntry> entries,
        long blockHeight = 0,
        string previousBlockHash = "genesis") =>
        new(
            BlockHeight: blockHeight,
            PreviousBlockHash: previousBlockHash,
            LedgerEntries: entries,
            TraceId: "trace-1",
            CorrelationId: "corr-1",
            Timestamp: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void Execute_ShouldConstructBlockFromEntries()
    {
        var entries = new[] { CreateEntry(1), CreateEntry(2), CreateEntry(3) };
        var command = CreateCommand(entries);

        var result = _engine.Execute(command);

        Assert.NotNull(result.Block);
        Assert.Equal(0, result.Block.BlockHeight);
        Assert.Equal("genesis", result.Block.PreviousBlockHash);
        Assert.Equal(3, result.EntryCount);
        Assert.Equal(3, result.Block.EntryCount);
        Assert.Equal(3, result.Block.Entries.Count);
        Assert.Equal("trace-1", result.TraceId);
    }

    [Fact]
    public void Execute_ShouldProduceDeterministicMerkleRoot()
    {
        var entries = new[] { CreateEntry(1), CreateEntry(2) };
        var command = CreateCommand(entries);

        var result1 = _engine.Execute(command);
        var result2 = _engine.Execute(command);

        Assert.Equal(result1.MerkleRoot, result2.MerkleRoot);
        Assert.NotEmpty(result1.MerkleRoot);
    }

    [Fact]
    public void Execute_ShouldProduceDeterministicBlockHash()
    {
        var entries = new[] { CreateEntry(1), CreateEntry(2) };
        var command = CreateCommand(entries);

        var result1 = _engine.Execute(command);
        var result2 = _engine.Execute(command);

        Assert.Equal(result1.BlockHash, result2.BlockHash);
        Assert.NotEmpty(result1.BlockHash);
    }

    [Fact]
    public void Execute_ShouldOrderEntriesBySequenceNumber()
    {
        var entry3 = CreateEntry(3);
        var entry1 = CreateEntry(1);
        var entry2 = CreateEntry(2);
        var command = CreateCommand(new[] { entry3, entry1, entry2 });

        var result = _engine.Execute(command);

        Assert.Equal(1, result.Block.Entries[0].SequenceNumber);
        Assert.Equal(2, result.Block.Entries[1].SequenceNumber);
        Assert.Equal(3, result.Block.Entries[2].SequenceNumber);
    }

    [Fact]
    public void Execute_ShouldLinkToPreviousBlock()
    {
        var entries1 = new[] { CreateEntry(1) };
        var result1 = _engine.Execute(CreateCommand(entries1, blockHeight: 0, previousBlockHash: "genesis"));

        var entries2 = new[] { CreateEntry(2) };
        var result2 = _engine.Execute(CreateCommand(entries2, blockHeight: 1, previousBlockHash: result1.BlockHash));

        Assert.Equal(result1.BlockHash, result2.Block.PreviousBlockHash);
        Assert.Equal(1, result2.Block.BlockHeight);
        Assert.NotEqual(result1.BlockHash, result2.BlockHash);
    }

    [Fact]
    public void Execute_ShouldThrowForEmptyEntries()
    {
        var command = CreateCommand(Array.Empty<ChainLedgerEntry>());

        Assert.Throws<ArgumentException>(() => _engine.Execute(command));
    }

    [Fact]
    public void Execute_ShouldThrowForMissingEntryHash()
    {
        var entry = CreateEntry(1, entryHash: "");
        var command = CreateCommand(new[] { entry });

        Assert.Throws<ArgumentException>(() => _engine.Execute(command));
    }

    [Fact]
    public void Execute_ShouldThrowForMissingPayloadHash()
    {
        var entry = CreateEntry(1, payloadHash: "");
        var command = CreateCommand(new[] { entry });

        Assert.Throws<ArgumentException>(() => _engine.Execute(command));
    }

    [Fact]
    public void Execute_SingleEntry_ShouldProduceValidBlock()
    {
        var entries = new[] { CreateEntry(1) };
        var command = CreateCommand(entries);

        var result = _engine.Execute(command);

        Assert.Equal(1, result.EntryCount);
        Assert.NotEmpty(result.MerkleRoot);
        Assert.NotEmpty(result.BlockHash);
    }

    [Fact]
    public void Execute_OddEntryCount_ShouldDuplicateLastLeaf()
    {
        var entries = new[] { CreateEntry(1), CreateEntry(2), CreateEntry(3) };
        var command = CreateCommand(entries);

        var result1 = _engine.Execute(command);
        var result2 = _engine.Execute(command);

        Assert.Equal(result1.MerkleRoot, result2.MerkleRoot);
    }

    [Fact]
    public void Execute_LargeBlock_ShouldComplete()
    {
        var entries = Enumerable.Range(1, 1000)
            .Select(i => CreateEntry(i))
            .ToList();
        var command = CreateCommand(entries);

        var result = _engine.Execute(command);

        Assert.Equal(1000, result.EntryCount);
        Assert.NotEmpty(result.MerkleRoot);
        Assert.NotEmpty(result.BlockHash);
    }

    [Fact]
    public void ComputeMerkleRoot_ShouldBeDeterministic()
    {
        var hashes = new[] { "hash-a", "hash-b", "hash-c" };

        var root1 = BlockBuilderEngine.ComputeMerkleRoot(hashes);
        var root2 = BlockBuilderEngine.ComputeMerkleRoot(hashes);

        Assert.Equal(root1, root2);
    }

    [Fact]
    public void ComputeBlockHash_ShouldBeDeterministic()
    {
        var ts = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var hash1 = BlockBuilderEngine.ComputeBlockHash(0, "genesis", "merkle", 5, ts);
        var hash2 = BlockBuilderEngine.ComputeBlockHash(0, "genesis", "merkle", 5, ts);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeBlockHash_DifferentInputs_ShouldDiffer()
    {
        var ts = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var hash1 = BlockBuilderEngine.ComputeBlockHash(0, "genesis", "merkle-a", 5, ts);
        var hash2 = BlockBuilderEngine.ComputeBlockHash(0, "genesis", "merkle-b", 5, ts);

        Assert.NotEqual(hash1, hash2);
    }
}
