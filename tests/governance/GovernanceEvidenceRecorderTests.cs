using Whycespace.Engines.T0U.Governance;
using Whycespace.Engines.T0U.WhyceChain;
using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.WhyceChain.Stores;

namespace Whycespace.Governance.Tests;

public class GovernanceEvidenceRecorderTests
{
    private readonly GovernanceEvidenceRecorder _recorder;
    private readonly ChainEvidenceGateway _gateway;

    public GovernanceEvidenceRecorderTests()
    {
        var ledgerStore = new ChainLedgerStore();
        var eventStore = new ChainEventStore();
        var ledgerEngine = new ChainLedgerEngine(ledgerStore);
        var hashEngine = new EvidenceHashEngine();
        var eventLedgerEngine = new ImmutableEventLedgerEngine(eventStore);
        var anchoringEngine = new EvidenceAnchoringEngine(ledgerEngine, hashEngine, eventLedgerEngine);
        _gateway = new ChainEvidenceGateway(anchoringEngine, hashEngine);
        _recorder = new GovernanceEvidenceRecorder(_gateway);
    }

    [Fact]
    public void RecordProposal_Succeeds()
    {
        var proposal = new GovernanceProposal(
            "p-1", "Fee Reduction", "Reduce fees", ProposalType.Policy,
            "g-alice", DateTime.UtcNow, ProposalStatus.Draft);

        var entry = _recorder.RecordProposal(proposal);

        Assert.Equal("gov-proposal-p-1", entry.EntryId);
        Assert.Equal("ProposalCreated", entry.EventType);
    }

    [Fact]
    public void RecordProposal_CanVerify()
    {
        var proposal = new GovernanceProposal(
            "p-2", "Title", "Desc", ProposalType.Constitutional,
            "g-bob", DateTime.UtcNow, ProposalStatus.Open);

        _recorder.RecordProposal(proposal);

        Assert.True(_gateway.VerifyEvidence("gov-proposal-p-2", proposal));
    }

    [Fact]
    public void RecordVote_Succeeds()
    {
        var vote = new GovernanceVote(
            "v-1", "p-1", "g-alice", VoteType.Approve, 1, DateTime.UtcNow);

        var entry = _recorder.RecordVote(vote);

        Assert.Equal("gov-vote-v-1", entry.EntryId);
        Assert.Equal("VoteCast", entry.EventType);
    }

    [Fact]
    public void RecordVote_CanVerify()
    {
        var vote = new GovernanceVote(
            "v-2", "p-1", "g-bob", VoteType.Reject, 1, DateTime.UtcNow);

        _recorder.RecordVote(vote);

        Assert.True(_gateway.VerifyEvidence("gov-vote-v-2", vote));
    }

    [Fact]
    public void RecordDecision_Succeeds()
    {
        var decision = new GovernanceDecision(
            "p-1", DecisionOutcome.Approved, 5, 2, 1, true);

        var entry = _recorder.RecordDecision(decision);

        Assert.Equal("gov-decision-p-1", entry.EntryId);
        Assert.Equal("DecisionMade", entry.EventType);
    }

    [Fact]
    public void RecordDecision_CanVerify()
    {
        var decision = new GovernanceDecision(
            "p-2", DecisionOutcome.Rejected, 1, 4, 0, true);

        _recorder.RecordDecision(decision);

        Assert.True(_gateway.VerifyEvidence("gov-decision-p-2", decision));
    }
}
