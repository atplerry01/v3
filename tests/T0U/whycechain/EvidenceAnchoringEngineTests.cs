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

public class EvidenceAnchoringEngineTests
{
    private readonly EvidenceAnchoringEngine _engine;
    private readonly ChainLedgerEngine _ledgerEngine;
    private readonly ImmutableEventLedgerEngine _eventLedgerEngine;

    public EvidenceAnchoringEngineTests()
    {
        var ledgerStore = new ChainLedgerStore();
        var eventStore = new ChainEventStore();
        _ledgerEngine = new ChainLedgerEngine(ledgerStore);
        var hashEngine = new EvidenceHashEngine();
        _eventLedgerEngine = new ImmutableEventLedgerEngine(eventStore);
        _engine = new EvidenceAnchoringEngine(_ledgerEngine, hashEngine, _eventLedgerEngine);
    }

    [Fact]
    public void AnchorEvidence_ShouldCreateLedgerEntryAndEvent()
    {
        var payload = new { Decision = "Approved", Amount = 1000 };

        var entry = _engine.AnchorEvidence("ev-1", "Finance", "Settlement", payload);

        Assert.Equal("ev-1", entry.EntryId);
        Assert.Equal("Settlement", entry.EventType);
        Assert.NotEmpty(entry.PayloadHash);

        var evt = _eventLedgerEngine.GetEvent("ev-1");
        Assert.Equal("Finance", evt.Domain);
    }

    [Fact]
    public void GetEvidenceProof_ShouldReturnHash()
    {
        var payload = new { Vote = "Yes" };
        _engine.AnchorEvidence("ev-1", "Governance", "Vote", payload);

        var proof = _engine.GetEvidenceProof("ev-1");

        Assert.Equal("SHA256", proof.Algorithm);
        Assert.NotEmpty(proof.Hash);
    }
}
