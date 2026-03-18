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

public class ChainIndexEngineTests
{
    private readonly ChainLedgerStore _ledgerStore;
    private readonly ChainBlockStore _blockStore;
    private readonly ChainLedgerEngine _ledgerEngine;
    private readonly ChainBlockEngine _blockEngine;
    private readonly MerkleProofEngine _merkleEngine;
    private readonly BlockBuilderEngine _builderEngine;
    private readonly ChainIndexEngine _engine;

    public ChainIndexEngineTests()
    {
        _ledgerStore = new ChainLedgerStore();
        _blockStore = new ChainBlockStore();
        var indexStore = new ChainIndexStore();
        _ledgerEngine = new ChainLedgerEngine(_ledgerStore);
        _blockEngine = new ChainBlockEngine(_blockStore);
        _merkleEngine = new MerkleProofEngine();
        _builderEngine = new BlockBuilderEngine(_ledgerStore, _blockEngine, _merkleEngine);
        _engine = new ChainIndexEngine(indexStore, _blockStore, _ledgerStore);
    }

    [Fact]
    public void IndexBlock_ShouldMakeEntriesSearchable()
    {
        _ledgerEngine.RegisterEntry("e-1", "PolicyDecision", "hash-1");
        _ledgerEngine.RegisterEntry("e-2", "Transaction", "hash-2");
        _builderEngine.BuildBlock();

        _engine.IndexBlock(0);

        var policyEntries = _engine.SearchEvents("PolicyDecision");
        var txEntries = _engine.SearchEvents("Transaction");

        Assert.Single(policyEntries);
        Assert.Single(txEntries);
        Assert.Equal("e-1", policyEntries[0].EntryId);
    }

    [Fact]
    public void SearchEvents_NoMatches_ShouldReturnEmpty()
    {
        var results = _engine.SearchEvents("NonExistent");

        Assert.Empty(results);
    }

    [Fact]
    public void IndexBlock_MultipleEntries_SameType_ShouldReturnAll()
    {
        _ledgerEngine.RegisterEntry("e-1", "PolicyDecision", "hash-1");
        _ledgerEngine.RegisterEntry("e-2", "PolicyDecision", "hash-2");
        _builderEngine.BuildBlock();

        _engine.IndexBlock(0);

        var results = _engine.SearchEvents("PolicyDecision");
        Assert.Equal(2, results.Count);
    }
}
