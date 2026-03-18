using Whycespace.Engines.T0U.WhyceChain;
using Whycespace.Systems.Upstream.WhyceChain.Models;
using Whycespace.Systems.Upstream.WhyceChain.Stores;

namespace Whycespace.WhyceChain.Tests;

public class ChainAuditEngineTests
{
    private readonly ChainLedgerStore _ledgerStore;
    private readonly ChainBlockStore _blockStore;
    private readonly ChainLedgerEngine _ledgerEngine;
    private readonly BlockBuilderEngine _builderEngine;
    private readonly ChainAuditEngine _engine;

    public ChainAuditEngineTests()
    {
        _ledgerStore = new ChainLedgerStore();
        _blockStore = new ChainBlockStore();
        _ledgerEngine = new ChainLedgerEngine(_ledgerStore);
        var blockEngine = new ChainBlockEngine(_blockStore);
        var merkleEngine = new MerkleProofEngine();
        _builderEngine = new BlockBuilderEngine(_ledgerStore, blockEngine, merkleEngine);
        var integrityEngine = new IntegrityVerificationEngine(merkleEngine);
        _engine = new ChainAuditEngine(_blockStore, _ledgerStore, integrityEngine);
    }

    [Fact]
    public void AuditChain_ValidChain_ShouldPass()
    {
        _ledgerEngine.RegisterEntry("e-1", "PolicyDecision", "hash-1");
        _builderEngine.BuildBlock();

        var result = _engine.AuditChain();

        Assert.True(result.Valid);
        Assert.Equal(1, result.BlocksAudited);
        Assert.Equal(1, result.EntriesAudited);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void AuditBlock_CorruptedBlock_ShouldFail()
    {
        var corrupt = new ChainBlock("bad", 0, "genesis", "fake", "wrong-root",
            DateTimeOffset.UtcNow, ["e-1"]);
        _blockStore.AddBlock(corrupt);

        var result = _engine.AuditBlock(0);

        Assert.False(result.Valid);
        Assert.NotEmpty(result.Issues);
    }

    [Fact]
    public void AuditEvent_UnanchoredEntry_ShouldReportIssue()
    {
        _ledgerEngine.RegisterEntry("e-1", "PolicyDecision", "hash-1");

        var result = _engine.AuditEvent("e-1");

        Assert.False(result.Valid);
        Assert.Contains(result.Issues, i => i.Contains("not yet anchored"));
    }

    [Fact]
    public void AuditEvent_AnchoredEntry_ShouldPass()
    {
        _ledgerEngine.RegisterEntry("e-1", "PolicyDecision", "hash-1");
        _builderEngine.BuildBlock();

        var result = _engine.AuditEvent("e-1");

        Assert.True(result.Valid);
        Assert.Empty(result.Issues);
    }
}
