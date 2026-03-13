using Whycespace.Engines.T0U.WhyceChain;
using Whycespace.System.Upstream.WhyceChain.Stores;

namespace Whycespace.WhyceChain.Tests;

public class ChainBlockEngineTests
{
    private readonly ChainBlockStore _store;
    private readonly ChainBlockEngine _engine;

    public ChainBlockEngineTests()
    {
        _store = new ChainBlockStore();
        _engine = new ChainBlockEngine(_store);
    }

    [Fact]
    public void CreateBlock_ShouldCreateGenesisBlock()
    {
        var block = _engine.CreateBlock(["entry-1", "entry-2"], "merkle-root-1");

        Assert.Equal(0, block.BlockNumber);
        Assert.Equal("genesis", block.PreviousBlockHash);
        Assert.Equal("merkle-root-1", block.MerkleRoot);
        Assert.Equal(2, block.EntryIds.Count);
    }

    [Fact]
    public void CreateBlock_ShouldChainSequentially()
    {
        var first = _engine.CreateBlock(["entry-1"], "merkle-1");
        var second = _engine.CreateBlock(["entry-2"], "merkle-2");

        Assert.Equal(0, first.BlockNumber);
        Assert.Equal(1, second.BlockNumber);
        Assert.Equal(first.BlockHash, second.PreviousBlockHash);
    }

    [Fact]
    public void ValidateBlock_ShouldRejectInvalidPreviousHash()
    {
        _engine.CreateBlock(["entry-1"], "merkle-1");

        var invalid = new Whycespace.System.Upstream.WhyceChain.Models.ChainBlock(
            "bad-block",
            1,
            "wrong-hash",
            "some-hash",
            "merkle-2",
            DateTimeOffset.UtcNow,
            ["entry-2"]);

        Assert.False(_engine.ValidateBlock(invalid));
    }

    [Fact]
    public void GetBlock_ShouldReturnStoredBlock()
    {
        var created = _engine.CreateBlock(["entry-1"], "merkle-1");

        var retrieved = _engine.GetBlock(0);

        Assert.Equal(created.BlockId, retrieved.BlockId);
        Assert.Equal(created.BlockHash, retrieved.BlockHash);
    }
}
