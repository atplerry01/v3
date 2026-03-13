using Whycespace.Engines.T0U.Governance;
using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;
using Whycespace.System.WhyceID.Aggregates;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;

namespace Whycespace.Governance.Tests;

public class GovernanceDecisionEngineTests
{
    private readonly IdentityRegistry _identityRegistry = new();
    private readonly GuardianRegistryStore _guardianStore = new();
    private readonly GovernanceProposalStore _proposalStore = new();
    private readonly GovernanceVoteStore _voteStore = new();
    private readonly GovernanceDecisionEngine _engine;
    private readonly VotingEngine _votingEngine;
    private readonly GovernanceProposalRegistryEngine _registryEngine;
    private readonly GovernanceProposalEngine _proposalEngine;
    private readonly GuardianRegistryEngine _guardianEngine;
    private readonly Guid _identityId;

    public GovernanceDecisionEngineTests()
    {
        _votingEngine = new VotingEngine(_voteStore, _proposalStore, _guardianStore);
        var quorumEngine = new QuorumEngine(_voteStore, _guardianStore, new QuorumConfig(50));
        _engine = new GovernanceDecisionEngine(_votingEngine, quorumEngine, _proposalStore);
        _registryEngine = new GovernanceProposalRegistryEngine(_proposalStore, _guardianStore);
        _proposalEngine = new GovernanceProposalEngine(_proposalStore);
        _guardianEngine = new GuardianRegistryEngine(_guardianStore, _identityRegistry);

        _identityId = Guid.NewGuid();
        _identityRegistry.Register(new IdentityAggregate(IdentityId.From(_identityId), IdentityType.User));
    }

    private string RegisterActiveGuardian(string id, string name)
    {
        _guardianEngine.RegisterGuardian(id, _identityId, name, new List<string>());
        _guardianEngine.ActivateGuardian(id);
        return id;
    }

    private string CreateVotingProposal(string id, string creatorId)
    {
        _registryEngine.CreateProposal(id, "Proposal", "Desc", ProposalType.Policy, creatorId);
        _proposalEngine.OpenProposal(id);
        _proposalEngine.StartVoting(id);
        return id;
    }

    [Fact]
    public void EvaluateDecision_Approved()
    {
        var g1 = RegisterActiveGuardian("g-1", "G1");
        var g2 = RegisterActiveGuardian("g-2", "G2");
        var proposalId = CreateVotingProposal("p-1", g1);

        _votingEngine.CastVote("v-1", proposalId, g1, VoteType.Approve);
        _votingEngine.CastVote("v-2", proposalId, g2, VoteType.Approve);

        var decision = _engine.EvaluateDecision(proposalId);

        Assert.Equal(DecisionOutcome.Approved, decision.Outcome);
        Assert.True(decision.QuorumMet);
        Assert.Equal(2, decision.Approve);
        Assert.Equal(0, decision.Reject);
    }

    [Fact]
    public void EvaluateDecision_Rejected()
    {
        var g1 = RegisterActiveGuardian("g-r1", "G1");
        var g2 = RegisterActiveGuardian("g-r2", "G2");
        var proposalId = CreateVotingProposal("p-rej", g1);

        _votingEngine.CastVote("v-1", proposalId, g1, VoteType.Reject);
        _votingEngine.CastVote("v-2", proposalId, g2, VoteType.Reject);

        var decision = _engine.EvaluateDecision(proposalId);

        Assert.Equal(DecisionOutcome.Rejected, decision.Outcome);
        Assert.True(decision.QuorumMet);
    }

    [Fact]
    public void EvaluateDecision_NoQuorum()
    {
        var g1 = RegisterActiveGuardian("g-nq1", "G1");
        RegisterActiveGuardian("g-nq2", "G2");
        RegisterActiveGuardian("g-nq3", "G3");
        RegisterActiveGuardian("g-nq4", "G4");
        var proposalId = CreateVotingProposal("p-nq", g1);

        _votingEngine.CastVote("v-1", proposalId, g1, VoteType.Approve);

        var decision = _engine.EvaluateDecision(proposalId);

        Assert.Equal(DecisionOutcome.NoQuorum, decision.Outcome);
        Assert.False(decision.QuorumMet);
    }

    [Fact]
    public void EvaluateDecision_Tie_Rejected()
    {
        var g1 = RegisterActiveGuardian("g-t1", "G1");
        var g2 = RegisterActiveGuardian("g-t2", "G2");
        var proposalId = CreateVotingProposal("p-tie", g1);

        _votingEngine.CastVote("v-1", proposalId, g1, VoteType.Approve);
        _votingEngine.CastVote("v-2", proposalId, g2, VoteType.Reject);

        var decision = _engine.EvaluateDecision(proposalId);

        Assert.Equal(DecisionOutcome.Rejected, decision.Outcome);
    }

    [Fact]
    public void EvaluateDecision_NotVoting_Throws()
    {
        var g1 = RegisterActiveGuardian("g-nv", "G1");
        _registryEngine.CreateProposal("p-draft", "Draft", "Desc", ProposalType.Policy, g1);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.EvaluateDecision("p-draft"));
        Assert.Contains("must be in Voting status", ex.Message);
    }

    [Fact]
    public void EvaluateDecision_NotFound_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.EvaluateDecision("nonexistent"));
        Assert.Contains("Proposal not found", ex.Message);
    }

    [Fact]
    public void DetermineOutcome_NoQuorum_ReturnsNoQuorum()
    {
        var outcome = _engine.DetermineOutcome(new VoteTally(10, 0, 0), false);

        Assert.Equal(DecisionOutcome.NoQuorum, outcome);
    }

    [Fact]
    public void DetermineOutcome_MoreApproves_ReturnsApproved()
    {
        var outcome = _engine.DetermineOutcome(new VoteTally(3, 2, 1), true);

        Assert.Equal(DecisionOutcome.Approved, outcome);
    }

    [Fact]
    public void DetermineOutcome_MoreRejects_ReturnsRejected()
    {
        var outcome = _engine.DetermineOutcome(new VoteTally(1, 3, 0), true);

        Assert.Equal(DecisionOutcome.Rejected, outcome);
    }
}
