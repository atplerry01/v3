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

public class ChainSnapshotEngineTests
{
    private readonly ChainSnapshotEngine _engine;
    private readonly ChainLedgerEngine _ledgerEngine;
    private readonly BlockBuilderEngine _builderEngine;

    public ChainSnapshotEngineTests()
    {
        var ledgerStore = new ChainLedgerStore();
        var blockStore = new ChainBlockStore();
        _ledgerEngine = new ChainLedgerEngine(ledgerStore);
        var blockEngine = new ChainBlockEngine(blockStore);
        var merkleEngine = new MerkleProofEngine();
        _builderEngine = new BlockBuilderEngine(ledgerStore, blockEngine, merkleEngine);
        _engine = new ChainSnapshotEngine(blockStore, ledgerStore);
    }

    [Fact]
    public void CreateSnapshot_ShouldCaptureChainState()
    {
        _ledgerEngine.RegisterEntry("e-1", "PolicyDecision", "hash-1");
        _builderEngine.BuildBlock();

        var snapshot = _engine.CreateSnapshot();

        Assert.Equal(0, snapshot.LatestBlockNumber);
        Assert.Equal(1, snapshot.TotalEntries);
        Assert.NotEqual("empty", snapshot.LatestBlockHash);
    }

    [Fact]
    public void RestoreSnapshot_ShouldReturnSavedState()
    {
        _ledgerEngine.RegisterEntry("e-1", "PolicyDecision", "hash-1");
        _builderEngine.BuildBlock();
        var snapshot = _engine.CreateSnapshot();

        var restored = _engine.RestoreSnapshot(snapshot.SnapshotId);

        Assert.Equal(snapshot.LatestBlockNumber, restored.LatestBlockNumber);
        Assert.Equal(snapshot.LatestBlockHash, restored.LatestBlockHash);
        Assert.Equal(snapshot.TotalEntries, restored.TotalEntries);
    }

    [Fact]
    public void CreateSnapshot_EmptyChain_ShouldReturnDefaults()
    {
        var snapshot = _engine.CreateSnapshot();

        Assert.Equal(-1, snapshot.LatestBlockNumber);
        Assert.Equal("empty", snapshot.LatestBlockHash);
        Assert.Equal(0, snapshot.TotalEntries);
    }
}
