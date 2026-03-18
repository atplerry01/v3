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
using Whycespace.Systems.Upstream.WhyceChain.Stores;

namespace Whycespace.WhyceChain.Tests;

public class ChainReplicationEngineTests
{
    private readonly ChainLedgerStore _ledgerStore;
    private readonly ChainLedgerEngine _ledgerEngine;
    private readonly BlockBuilderEngine _builderEngine;
    private readonly ChainReplicationEngine _engine;

    public ChainReplicationEngineTests()
    {
        _ledgerStore = new ChainLedgerStore();
        var blockStore = new ChainBlockStore();
        _ledgerEngine = new ChainLedgerEngine(_ledgerStore);
        var blockEngine = new ChainBlockEngine(blockStore);
        var merkleEngine = new MerkleProofEngine();
        _builderEngine = new BlockBuilderEngine(_ledgerStore, blockEngine, merkleEngine);
        var integrityEngine = new IntegrityVerificationEngine(merkleEngine);
        _engine = new ChainReplicationEngine(blockStore, integrityEngine);
    }

    [Fact]
    public void ReplicateBlock_ShouldReturnBlockAndTrackNode()
    {
        _ledgerEngine.RegisterEntry("e-1", "PolicyDecision", "hash-1");
        _builderEngine.BuildBlock();

        var block = _engine.ReplicateBlock("node-1", 0);

        Assert.Equal(0, block.BlockNumber);
        Assert.True(_engine.VerifyNode("node-1"));
    }

    [Fact]
    public void SyncNode_ShouldReplicateAllMissingBlocks()
    {
        _ledgerEngine.RegisterEntry("e-1", "PolicyDecision", "hash-1");
        _builderEngine.BuildBlock();
        _ledgerEngine.RegisterEntry("e-2", "Transaction", "hash-2");
        _builderEngine.BuildBlock();

        var blocks = _engine.SyncNode("node-1");

        Assert.Equal(2, blocks.Count);
        Assert.Equal(0, blocks[0].BlockNumber);
        Assert.Equal(1, blocks[1].BlockNumber);
    }

    [Fact]
    public void SyncNode_AlreadySynced_ShouldReturnEmpty()
    {
        _ledgerEngine.RegisterEntry("e-1", "PolicyDecision", "hash-1");
        _builderEngine.BuildBlock();

        _engine.SyncNode("node-1");
        var second = _engine.SyncNode("node-1");

        Assert.Empty(second);
    }

    [Fact]
    public void VerifyNode_UnknownNode_ShouldReturnFalse()
    {
        Assert.False(_engine.VerifyNode("unknown"));
    }

    [Fact]
    public void VerifyNode_SyncedNode_ShouldConverge()
    {
        _ledgerEngine.RegisterEntry("e-1", "PolicyDecision", "hash-1");
        _builderEngine.BuildBlock();

        _engine.SyncNode("node-a");
        _engine.SyncNode("node-b");

        Assert.True(_engine.VerifyNode("node-a"));
        Assert.True(_engine.VerifyNode("node-b"));
    }
}
