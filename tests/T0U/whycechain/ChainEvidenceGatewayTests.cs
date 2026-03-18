using Whycespace.Engines.T0U.WhyceChain;
using Whycespace.Systems.Upstream.WhyceChain.Stores;

namespace Whycespace.WhyceChain.Tests;

public class ChainEvidenceGatewayTests
{
    private readonly ChainEvidenceGateway _gateway;

    public ChainEvidenceGatewayTests()
    {
        var ledgerStore = new ChainLedgerStore();
        var eventStore = new ChainEventStore();
        var ledgerEngine = new ChainLedgerEngine(ledgerStore);
        var hashEngine = new EvidenceHashEngine();
        var eventLedgerEngine = new ImmutableEventLedgerEngine(eventStore);
        var anchoringEngine = new EvidenceAnchoringEngine(ledgerEngine, hashEngine, eventLedgerEngine);
        _gateway = new ChainEvidenceGateway(anchoringEngine, hashEngine);
    }

    [Fact]
    public void SubmitEvidence_ShouldReturnLedgerEntry()
    {
        var payload = new { Action = "PolicyApproved", PolicyId = "p-1" };

        var entry = _gateway.SubmitEvidence("ev-1", "Policy", "PolicyDecision", payload);

        Assert.Equal("ev-1", entry.EntryId);
        Assert.Equal("PolicyDecision", entry.EventType);
    }

    [Fact]
    public void GetEvidence_ShouldReturnProof()
    {
        var payload = new { Amount = 500 };
        _gateway.SubmitEvidence("ev-1", "Finance", "Settlement", payload);

        var proof = _gateway.GetEvidence("ev-1");

        Assert.Equal("SHA256", proof.Algorithm);
        Assert.NotEmpty(proof.Hash);
    }

    [Fact]
    public void VerifyEvidence_ShouldConfirmIntegrity()
    {
        var payload = new { Vote = "Yes", ProposalId = "prop-1" };
        _gateway.SubmitEvidence("ev-1", "Governance", "Vote", payload);

        Assert.True(_gateway.VerifyEvidence("ev-1", payload));
        Assert.False(_gateway.VerifyEvidence("ev-1", new { Vote = "No", ProposalId = "prop-1" }));
    }
}
