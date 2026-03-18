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
using Whycespace.Systems.Upstream.WhyceChain.Models;
using Whycespace.Systems.Upstream.WhyceChain.Stores;

namespace Whycespace.WhyceChain.Tests;

public class IntegrityVerificationEngineTests
{
    private readonly ChainBlockStore _blockStore;
    private readonly ChainBlockEngine _blockEngine;
    private readonly MerkleProofEngine _merkleEngine;
    private readonly IntegrityVerificationEngine _engine;

    public IntegrityVerificationEngineTests()
    {
        _blockStore = new ChainBlockStore();
        _blockEngine = new ChainBlockEngine(_blockStore);
        _merkleEngine = new MerkleProofEngine();
        _engine = new IntegrityVerificationEngine(_merkleEngine);
    }

    [Fact]
    public void Execute_ValidChain_ShouldReturnTrue()
    {
        var entries = new[] { "entry-1", "entry-2" };
        var merkleRoot = _merkleEngine.BuildTree(entries);
        var block0 = _blockEngine.CreateBlock(entries, merkleRoot);

        var entries2 = new[] { "entry-3" };
        var merkleRoot2 = _merkleEngine.BuildTree(entries2);
        var block1 = _blockEngine.CreateBlock(entries2, merkleRoot2);

        var command = new IntegrityVerificationCommand(
            Array.Empty<ChainLedgerEntry>(),
            [block0, block1],
            MerkleProof: null,
            TraceId: "test",
            CorrelationId: "test",
            Timestamp: DateTimeOffset.UtcNow);

        var result = _engine.Execute(command);

        Assert.True(result.BlockChainValid);
        Assert.True(result.MerkleRootValid);
    }

    [Fact]
    public void Execute_CorruptedBlock_ShouldReturnFalse()
    {
        var entries = new[] { "entry-1", "entry-2" };
        var merkleRoot = _merkleEngine.BuildTree(entries);
        var block0 = _blockEngine.CreateBlock(entries, merkleRoot);

        var corrupt = new ChainBlock(
            "corrupt",
            1,
            block0.BlockHash,
            "fake-hash",
            "wrong-merkle-root",
            DateTimeOffset.UtcNow,
            ["entry-3"]);

        var command = new IntegrityVerificationCommand(
            Array.Empty<ChainLedgerEntry>(),
            [block0, corrupt],
            MerkleProof: null,
            TraceId: "test",
            CorrelationId: "test",
            Timestamp: DateTimeOffset.UtcNow);

        var result = _engine.Execute(command);

        Assert.False(result.MerkleRootValid);
    }

    [Fact]
    public void Execute_InvalidBlock_ShouldReturnFalse()
    {
        var corrupt = new ChainBlock(
            "corrupt",
            0,
            "genesis",
            "fake-hash",
            "wrong-merkle-root",
            DateTimeOffset.UtcNow,
            ["entry-1"]);

        var command = new IntegrityVerificationCommand(
            Array.Empty<ChainLedgerEntry>(),
            [corrupt],
            MerkleProof: null,
            TraceId: "test",
            CorrelationId: "test",
            Timestamp: DateTimeOffset.UtcNow);

        var result = _engine.Execute(command);

        Assert.False(result.BlockChainValid);
        Assert.False(result.MerkleRootValid);
    }
}
