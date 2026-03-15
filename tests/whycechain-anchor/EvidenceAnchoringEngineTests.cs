using Whycespace.Engines.T0U.WhyceChain;
using Whycespace.System.Upstream.WhyceChain.Ledger;
using Whycespace.System.Upstream.WhyceChain.Stores;

namespace Whycespace.WhyceChain.Anchor.Tests;

public class EvidenceAnchoringEngineTests
{
    private readonly EvidenceAnchoringEngine _engine;

    public EvidenceAnchoringEngineTests()
    {
        var ledgerStore = new ChainLedgerStore();
        var eventStore = new ChainEventStore();
        var ledgerEngine = new ChainLedgerEngine(ledgerStore);
        var hashEngine = new EvidenceHashEngine();
        var eventLedgerEngine = new ImmutableEventLedgerEngine(eventStore);
        _engine = new EvidenceAnchoringEngine(ledgerEngine, hashEngine, eventLedgerEngine);
    }

    private static ChainBlock CreateTestBlock(
        long blockHeight = 42,
        string blockHash = "abc123hash",
        string merkleRoot = "merkle-root-xyz",
        int entryCount = 5,
        DateTime? createdAt = null)
    {
        return new ChainBlock(
            BlockId: Guid.NewGuid(),
            BlockHeight: blockHeight,
            PreviousBlockHash: "prev-hash",
            Entries: [],
            MerkleRoot: merkleRoot,
            BlockHash: blockHash,
            EntryCount: entryCount,
            CreatedAt: createdAt ?? new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            ValidatorSignature: "sig",
            TraceId: "trace-1");
    }

    [Fact]
    public void Execute_ShouldGenerateAnchorPayload()
    {
        var block = CreateTestBlock();
        var command = new EvidenceAnchorCommand(
            Block: block,
            AnchorTarget: "ethereum",
            TraceId: "trace-1",
            CorrelationId: "corr-1",
            Timestamp: new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc));

        var result = _engine.Execute(command);

        Assert.Equal(block.BlockHash, result.BlockHash);
        Assert.Equal(block.MerkleRoot, result.MerkleRoot);
        Assert.Equal("ethereum", result.AnchorTarget);
        Assert.Equal("trace-1", result.TraceId);
        Assert.NotEmpty(result.AnchorPayload);
        Assert.NotEmpty(result.AnchorPayloadHash);
        Assert.NotEmpty(result.AnchorReferenceId);
    }

    [Fact]
    public void Execute_ShouldProduceDeterministicPayload()
    {
        var fixedTime = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var block = CreateTestBlock(createdAt: fixedTime);

        var command = new EvidenceAnchorCommand(
            Block: block,
            AnchorTarget: "bitcoin",
            TraceId: "trace-1",
            CorrelationId: "corr-1",
            Timestamp: DateTime.UtcNow);

        var result1 = _engine.Execute(command);
        var result2 = _engine.Execute(command);

        Assert.Equal(result1.AnchorPayload, result2.AnchorPayload);
        Assert.Equal(result1.AnchorPayloadHash, result2.AnchorPayloadHash);
        Assert.Equal(result1.AnchorReferenceId, result2.AnchorReferenceId);
    }

    [Fact]
    public void Execute_IdenticalBlocks_ShouldProduceIdenticalAnchorPayload()
    {
        var fixedTime = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var block1 = new ChainBlock(
            BlockId: Guid.NewGuid(),
            BlockHeight: 100,
            PreviousBlockHash: "prev",
            Entries: [],
            MerkleRoot: "merkle-abc",
            BlockHash: "hash-abc",
            EntryCount: 10,
            CreatedAt: fixedTime,
            ValidatorSignature: "sig-1",
            TraceId: "t1");

        var block2 = new ChainBlock(
            BlockId: Guid.NewGuid(),
            BlockHeight: 100,
            PreviousBlockHash: "different-prev",
            Entries: [],
            MerkleRoot: "merkle-abc",
            BlockHash: "hash-abc",
            EntryCount: 10,
            CreatedAt: fixedTime,
            ValidatorSignature: "sig-2",
            TraceId: "t2");

        var cmd1 = new EvidenceAnchorCommand(block1, "ethereum", "t1", "c1", DateTime.UtcNow);
        var cmd2 = new EvidenceAnchorCommand(block2, "ethereum", "t2", "c2", DateTime.UtcNow);

        var result1 = _engine.Execute(cmd1);
        var result2 = _engine.Execute(cmd2);

        Assert.Equal(result1.AnchorPayload, result2.AnchorPayload);
        Assert.Equal(result1.AnchorPayloadHash, result2.AnchorPayloadHash);
    }

    [Fact]
    public void Execute_ShouldContainExpectedPayloadFields()
    {
        var block = CreateTestBlock(blockHeight: 99, entryCount: 7);
        var command = new EvidenceAnchorCommand(block, "timestamping-service", "t", "c", DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.Contains("blockHeight", result.AnchorPayload);
        Assert.Contains("blockHash", result.AnchorPayload);
        Assert.Contains("merkleRoot", result.AnchorPayload);
        Assert.Contains("entryCount", result.AnchorPayload);
        Assert.Contains("createdAt", result.AnchorPayload);
    }

    [Fact]
    public void Execute_MissingMerkleRoot_ShouldThrow()
    {
        var block = new ChainBlock(
            Guid.NewGuid(), 1, "prev", [], "", "hash", 0,
            DateTime.UtcNow, "sig", "t");
        var command = new EvidenceAnchorCommand(block, "ethereum", "t", "c", DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => _engine.Execute(command));
    }

    [Fact]
    public void Execute_MissingBlockHash_ShouldThrow()
    {
        var block = new ChainBlock(
            Guid.NewGuid(), 1, "prev", [], "merkle", "", 0,
            DateTime.UtcNow, "sig", "t");
        var command = new EvidenceAnchorCommand(block, "ethereum", "t", "c", DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => _engine.Execute(command));
    }

    [Fact]
    public void Execute_MissingAnchorTarget_ShouldThrow()
    {
        var block = CreateTestBlock();
        var command = new EvidenceAnchorCommand(block, "", "t", "c", DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => _engine.Execute(command));
    }

    [Fact]
    public void Execute_ShouldBeConcurrencySafe()
    {
        var block = CreateTestBlock();
        var exceptions = new List<Exception>();
        var results = new EvidenceAnchorResult[10];

        Parallel.For(0, 10, i =>
        {
            try
            {
                var command = new EvidenceAnchorCommand(
                    block, "governance-anchor", $"trace-{i}", $"corr-{i}", DateTime.UtcNow);
                results[i] = _engine.Execute(command);
            }
            catch (Exception ex)
            {
                lock (exceptions) { exceptions.Add(ex); }
            }
        });

        Assert.Empty(exceptions);
        var payloads = results.Select(r => r.AnchorPayload).Distinct().ToList();
        Assert.Single(payloads);

        var hashes = results.Select(r => r.AnchorPayloadHash).Distinct().ToList();
        Assert.Single(hashes);
    }
}
