using Whycespace.Engines.T0U.WhyceChain;
using Whycespace.System.Upstream.WhyceChain.Models;
using Whycespace.System.Upstream.WhyceChain.Stores;

namespace Whycespace.WhyceChain.Tests;

public class IntegrityVerificationEngineTests
{
    private readonly ChainBlockStore _blockStore;
    private readonly ChainLedgerStore _ledgerStore;
    private readonly ChainBlockEngine _blockEngine;
    private readonly ChainLedgerEngine _ledgerEngine;
    private readonly MerkleProofEngine _merkleEngine;
    private readonly IntegrityVerificationEngine _engine;

    public IntegrityVerificationEngineTests()
    {
        _blockStore = new ChainBlockStore();
        _ledgerStore = new ChainLedgerStore();
        _blockEngine = new ChainBlockEngine(_blockStore);
        _ledgerEngine = new ChainLedgerEngine(_ledgerStore);
        _merkleEngine = new MerkleProofEngine();
        _engine = new IntegrityVerificationEngine(_blockStore, _ledgerStore, _merkleEngine);
    }

    [Fact]
    public void VerifyChain_ValidChain_ShouldReturnTrue()
    {
        var entries = new[] { "entry-1", "entry-2" };
        var merkleRoot = _merkleEngine.BuildTree(entries);
        _blockEngine.CreateBlock(entries, merkleRoot);

        var entries2 = new[] { "entry-3" };
        var merkleRoot2 = _merkleEngine.BuildTree(entries2);
        _blockEngine.CreateBlock(entries2, merkleRoot2);

        Assert.True(_engine.VerifyChain());
    }

    [Fact]
    public void VerifyChain_CorruptedBlock_ShouldReturnFalse()
    {
        var entries = new[] { "entry-1", "entry-2" };
        var merkleRoot = _merkleEngine.BuildTree(entries);
        _blockEngine.CreateBlock(entries, merkleRoot);

        // Insert a block with wrong merkle root
        var latest = _blockStore.GetLatestBlock()!;
        var corrupt = new ChainBlock(
            "corrupt",
            1,
            latest.BlockHash,
            "fake-hash",
            "wrong-merkle-root",
            DateTimeOffset.UtcNow,
            ["entry-3"]);
        _blockStore.AddBlock(corrupt);

        Assert.False(_engine.VerifyChain());
    }

    [Fact]
    public void VerifyBlock_InvalidBlock_ShouldReturnFalse()
    {
        var corrupt = new ChainBlock(
            "corrupt",
            0,
            "genesis",
            "fake-hash",
            "wrong-merkle-root",
            DateTimeOffset.UtcNow,
            ["entry-1"]);
        _blockStore.AddBlock(corrupt);

        Assert.False(_engine.VerifyBlock(0));
    }
}
