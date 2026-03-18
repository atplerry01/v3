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

public class BlockBuilderEngineTests
{
    private readonly ChainLedgerStore _ledgerStore;
    private readonly ChainBlockStore _blockStore;
    private readonly ChainLedgerEngine _ledgerEngine;
    private readonly ChainBlockEngine _blockEngine;
    private readonly MerkleProofEngine _merkleEngine;
    private readonly BlockBuilderEngine _engine;

    public BlockBuilderEngineTests()
    {
        _ledgerStore = new ChainLedgerStore();
        _blockStore = new ChainBlockStore();
        _ledgerEngine = new ChainLedgerEngine(_ledgerStore);
        _blockEngine = new ChainBlockEngine(_blockStore);
        _merkleEngine = new MerkleProofEngine();
        _engine = new BlockBuilderEngine(_ledgerStore, _blockEngine, _merkleEngine);
    }

    [Fact]
    public void CollectPendingEntries_ShouldReturnUnblockedEntries()
    {
        _ledgerEngine.RegisterEntry("e-1", "PolicyDecision", "hash-1");
        _ledgerEngine.RegisterEntry("e-2", "Transaction", "hash-2");

        var pending = _engine.CollectPendingEntries();

        Assert.Equal(2, pending.Count);
    }

    [Fact]
    public void BuildBlock_ShouldBatchEntriesIntoBlock()
    {
        _ledgerEngine.RegisterEntry("e-1", "PolicyDecision", "hash-1");
        _ledgerEngine.RegisterEntry("e-2", "Transaction", "hash-2");

        var block = _engine.BuildBlock();

        Assert.NotNull(block);
        Assert.Equal(2, block.EntryIds.Count);
        Assert.Contains("e-1", block.EntryIds);
        Assert.Contains("e-2", block.EntryIds);
    }

    [Fact]
    public void BuildBlock_ShouldSetBlockIdOnEntries()
    {
        _ledgerEngine.RegisterEntry("e-1", "PolicyDecision", "hash-1");

        var block = _engine.BuildBlock()!;

        var entry = _ledgerStore.GetEntry("e-1");
        Assert.Equal(block.BlockId, entry.BlockId);
    }

    [Fact]
    public void BuildBlock_ShouldVerifyMerkleRoot()
    {
        _ledgerEngine.RegisterEntry("e-1", "PolicyDecision", "hash-1");
        _ledgerEngine.RegisterEntry("e-2", "Transaction", "hash-2");

        var block = _engine.BuildBlock()!;
        var expectedRoot = _merkleEngine.BuildTree(block.EntryIds);

        Assert.Equal(expectedRoot, block.MerkleRoot);
    }

    [Fact]
    public void BuildBlock_NoPending_ShouldReturnNull()
    {
        var block = _engine.BuildBlock();

        Assert.Null(block);
    }
}
