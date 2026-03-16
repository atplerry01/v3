using Whycespace.Engines.T0U.Governance;
using Whycespace.Engines.T0U.WhyceChain;
using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;
using Whycespace.Systems.Upstream.WhyceChain.Stores;
using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;

namespace Whycespace.Governance.Tests;

public class GovernanceAuditEngineTests
{
    private readonly GovernanceProposalStore _proposalStore = new();
    private readonly GovernanceVoteStore _voteStore = new();
    private readonly GuardianRegistryStore _guardianStore = new();
    private readonly ChainEvidenceGateway _gateway;
    private readonly GovernanceAuditEngine _auditEngine;
    private readonly GovernanceEvidenceRecorder _recorder;
    private readonly GovernanceProposalRegistryEngine _registryEngine;
    private readonly GovernanceProposalEngine _proposalEngine;

    public GovernanceAuditEngineTests()
    {
        var ledgerStore = new ChainLedgerStore();
        var eventStore = new ChainEventStore();
        var ledgerEngine = new ChainLedgerEngine(ledgerStore);
        var hashEngine = new EvidenceHashEngine();
        var eventLedgerEngine = new ImmutableEventLedgerEngine(eventStore);
        var anchoringEngine = new EvidenceAnchoringEngine(ledgerEngine, hashEngine, eventLedgerEngine);
        _gateway = new ChainEvidenceGateway(anchoringEngine, hashEngine);
        _recorder = new GovernanceEvidenceRecorder(_gateway);
        _auditEngine = new GovernanceAuditEngine(_proposalStore, _voteStore, _gateway);
        _registryEngine = new GovernanceProposalRegistryEngine(_proposalStore, _guardianStore);
        _proposalEngine = new GovernanceProposalEngine(_proposalStore);

        var identityRegistry = new IdentityRegistry();
        var identityId = Guid.NewGuid();
        identityRegistry.Register(new IdentityAggregate(IdentityId.From(identityId), IdentityType.User));
        var guardianEngine = new GuardianRegistryEngine(_guardianStore, identityRegistry);
        guardianEngine.RegisterGuardian("g-alice", identityId, "Alice", new List<string>());
        guardianEngine.ActivateGuardian("g-alice");
        guardianEngine.RegisterGuardian("g-bob", identityId, "Bob", new List<string>());
        guardianEngine.ActivateGuardian("g-bob");
    }

    [Fact]
    public void AuditProposal_WithEvidence_IsValid()
    {
        var proposal = _registryEngine.CreateProposal("p-1", "Title", "Desc", ProposalType.Policy, "g-alice");
        _recorder.RecordProposal(proposal);

        var result = _auditEngine.AuditProposal("p-1");

        Assert.True(result.HasEvidence);
        Assert.True(result.IsValid);
        Assert.Empty(result.Findings);
        Assert.Equal(AuditTargetType.Proposal, result.TargetType);
    }

    [Fact]
    public void AuditProposal_WithoutEvidence_NotValid()
    {
        _registryEngine.CreateProposal("p-noev", "Title", "Desc", ProposalType.Policy, "g-alice");

        var result = _auditEngine.AuditProposal("p-noev");

        Assert.False(result.HasEvidence);
        Assert.False(result.IsValid);
        Assert.Contains(result.Findings, f => f.Contains("No evidence recorded"));
    }

    [Fact]
    public void AuditProposal_NotFound_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _auditEngine.AuditProposal("nonexistent"));
        Assert.Contains("Proposal not found", ex.Message);
    }

    [Fact]
    public void AuditVotes_WithEvidence_AllValid()
    {
        _registryEngine.CreateProposal("p-v", "Title", "Desc", ProposalType.Policy, "g-alice");
        _proposalEngine.OpenProposal("p-v");
        _proposalEngine.StartVoting("p-v");

        var vote1 = new GovernanceVote("v-1", "p-v", "g-alice", VoteType.Approve, 1, DateTime.UtcNow);
        var vote2 = new GovernanceVote("v-2", "p-v", "g-bob", VoteType.Reject, 1, DateTime.UtcNow);
        _voteStore.Add(vote1);
        _voteStore.Add(vote2);
        _recorder.RecordVote(vote1);
        _recorder.RecordVote(vote2);

        var results = _auditEngine.AuditVotes("p-v");

        Assert.Equal(2, results.Count);
        Assert.All(results, r =>
        {
            Assert.True(r.HasEvidence);
            Assert.True(r.IsValid);
            Assert.Equal(AuditTargetType.Vote, r.TargetType);
        });
    }

    [Fact]
    public void AuditVotes_WithoutEvidence_NotValid()
    {
        _registryEngine.CreateProposal("p-vnoe", "Title", "Desc", ProposalType.Policy, "g-alice");
        _proposalEngine.OpenProposal("p-vnoe");
        _proposalEngine.StartVoting("p-vnoe");
        _voteStore.Add(new GovernanceVote("v-noe", "p-vnoe", "g-alice", VoteType.Approve, 1, DateTime.UtcNow));

        var results = _auditEngine.AuditVotes("p-vnoe");

        Assert.Single(results);
        Assert.False(results[0].HasEvidence);
        Assert.False(results[0].IsValid);
    }

    [Fact]
    public void AuditDecision_WithEvidence_IsValid()
    {
        _registryEngine.CreateProposal("p-d", "Title", "Desc", ProposalType.Policy, "g-alice");
        var decision = new GovernanceDecision("p-d", DecisionOutcome.Approved, 3, 1, 0, true);
        _recorder.RecordDecision(decision);

        var result = _auditEngine.AuditDecision("p-d");

        Assert.True(result.HasEvidence);
        Assert.True(result.IsValid);
        Assert.Equal(AuditTargetType.Decision, result.TargetType);
    }

    [Fact]
    public void AuditDecision_WithoutEvidence_NotValid()
    {
        _registryEngine.CreateProposal("p-dnoe", "Title", "Desc", ProposalType.Policy, "g-alice");

        var result = _auditEngine.AuditDecision("p-dnoe");

        Assert.False(result.HasEvidence);
        Assert.False(result.IsValid);
        Assert.Contains(result.Findings, f => f.Contains("No evidence recorded"));
    }

    [Fact]
    public void AllGovernanceActions_Traceable()
    {
        // End-to-end: create proposal, vote, decide, record all, audit all
        // Evidence is recorded at each stage — proposal evidence captures the Draft state
        var proposal = _registryEngine.CreateProposal("p-e2e", "E2E", "Full audit", ProposalType.Constitutional, "g-alice");
        _recorder.RecordProposal(proposal);

        _proposalEngine.OpenProposal("p-e2e");
        _proposalEngine.StartVoting("p-e2e");

        var vote = new GovernanceVote("v-e2e", "p-e2e", "g-alice", VoteType.Approve, 1, DateTime.UtcNow);
        _voteStore.Add(vote);
        _recorder.RecordVote(vote);

        var decision = new GovernanceDecision("p-e2e", DecisionOutcome.Approved, 1, 0, 0, true);
        _recorder.RecordDecision(decision);

        // Proposal evidence exists (hash may differ since status changed after recording)
        var proposalAudit = _auditEngine.AuditProposal("p-e2e");
        Assert.True(proposalAudit.HasEvidence);

        // Vote and decision evidence is valid (unchanged after recording)
        var voteAudits = _auditEngine.AuditVotes("p-e2e");
        Assert.All(voteAudits, r => Assert.True(r.IsValid));

        var decisionAudit = _auditEngine.AuditDecision("p-e2e");
        Assert.True(decisionAudit.IsValid);
    }
}
