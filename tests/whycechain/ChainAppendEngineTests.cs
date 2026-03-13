using Whycespace.Engines.T0U.WhyceChain;
using Whycespace.System.Upstream.WhyceChain.Stores;

namespace Whycespace.WhyceChain.Tests;

public class ChainAppendEngineTests
{
    private readonly ChainLedgerStore _ledgerStore;
    private readonly ChainBlockStore _blockStore;
    private readonly ChainLedgerEngine _ledgerEngine;
    private readonly ChainAppendEngine _engine;

    public ChainAppendEngineTests()
    {
        _ledgerStore = new ChainLedgerStore();
        _blockStore = new ChainBlockStore();
        _ledgerEngine = new ChainLedgerEngine(_ledgerStore);
        var blockEngine = new ChainBlockEngine(_blockStore);
        var merkleEngine = new MerkleProofEngine();
        var builderEngine = new BlockBuilderEngine(_ledgerStore, blockEngine, merkleEngine);
        var integrityEngine = new IntegrityVerificationEngine(_blockStore, _ledgerStore, merkleEngine);
        _engine = new ChainAppendEngine(_blockStore, builderEngine, integrityEngine);
    }

    [Fact]
    public void AppendBlock_ShouldBuildAndValidate()
    {
        _ledgerEngine.RegisterEntry("e-1", "PolicyDecision", "hash-1");
        _ledgerEngine.RegisterEntry("e-2", "Transaction", "hash-2");

        var block = _engine.AppendBlock();

        Assert.NotNull(block);
        Assert.Equal(0, block.BlockNumber);
        Assert.Equal("genesis", block.PreviousBlockHash);
    }

    [Fact]
    public void AppendBlock_Sequential_ShouldChain()
    {
        _ledgerEngine.RegisterEntry("e-1", "PolicyDecision", "hash-1");
        var first = _engine.AppendBlock()!;

        _ledgerEngine.RegisterEntry("e-2", "Transaction", "hash-2");
        var second = _engine.AppendBlock()!;

        Assert.Equal(0, first.BlockNumber);
        Assert.Equal(1, second.BlockNumber);
        Assert.Equal(first.BlockHash, second.PreviousBlockHash);
    }

    [Fact]
    public void AppendBlock_NoPending_ShouldReturnNull()
    {
        var block = _engine.AppendBlock();

        Assert.Null(block);
    }
}
