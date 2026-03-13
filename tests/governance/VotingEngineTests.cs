using Whycespace.Engines.T0U.Governance;
using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;
using Whycespace.System.WhyceID.Aggregates;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;

namespace Whycespace.Governance.Tests;

public class VotingEngineTests
{
    private readonly IdentityRegistry _identityRegistry = new();
    private readonly GuardianRegistryStore _guardianStore = new();
    private readonly GovernanceProposalStore _proposalStore = new();
    private readonly GovernanceVoteStore _voteStore = new();
    private readonly VotingEngine _engine;
    private readonly GovernanceProposalRegistryEngine _registryEngine;
    private readonly GovernanceProposalEngine _proposalEngine;
    private readonly GuardianRegistryEngine _guardianEngine;
    private readonly Guid _identityId;

    public VotingEngineTests()
    {
        _engine = new VotingEngine(_voteStore, _proposalStore, _guardianStore);
        _registryEngine = new GovernanceProposalRegistryEngine(_proposalStore, _guardianStore);
        _proposalEngine = new GovernanceProposalEngine(_proposalStore);
        _guardianEngine = new GuardianRegistryEngine(_guardianStore, _identityRegistry);

        _identityId = Guid.NewGuid();
        _identityRegistry.Register(new IdentityAggregate(IdentityId.From(_identityId), IdentityType.User));

        _guardianEngine.RegisterGuardian("g-alice", _identityId, "Alice", new List<string>());
        _guardianEngine.ActivateGuardian("g-alice");

        _guardianEngine.RegisterGuardian("g-bob", _identityId, "Bob", new List<string>());
        _guardianEngine.ActivateGuardian("g-bob");

        _registryEngine.CreateProposal("p-1", "Test Proposal", "Desc", ProposalType.Policy, "g-alice");
        _proposalEngine.OpenProposal("p-1");
        _proposalEngine.StartVoting("p-1");
    }

    [Fact]
    public void CastVote_Succeeds()
    {
        var vote = _engine.CastVote("v-1", "p-1", "g-alice", VoteType.Approve);

        Assert.Equal("v-1", vote.VoteId);
        Assert.Equal("p-1", vote.ProposalId);
        Assert.Equal("g-alice", vote.GuardianId);
        Assert.Equal(VoteType.Approve, vote.Vote);
    }

    [Fact]
    public void CastVote_GuardianMayVoteOnce()
    {
        _engine.CastVote("v-1", "p-1", "g-alice", VoteType.Approve);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.CastVote("v-2", "p-1", "g-alice", VoteType.Reject));
        Assert.Contains("already voted", ex.Message);
    }

    [Fact]
    public void CastVote_InactiveGuardian_Throws()
    {
        _guardianEngine.RegisterGuardian("g-inactive", _identityId, "Inactive", new List<string>());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.CastVote("v-1", "p-1", "g-inactive", VoteType.Approve));
        Assert.Contains("Inactive guardians cannot vote", ex.Message);
    }

    [Fact]
    public void CastVote_ProposalNotVoting_Throws()
    {
        _registryEngine.CreateProposal("p-draft", "Draft", "Desc", ProposalType.Policy, "g-alice");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.CastVote("v-1", "p-draft", "g-alice", VoteType.Approve));
        Assert.Contains("not in Voting status", ex.Message);
    }

    [Fact]
    public void CastVote_InvalidProposal_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.CastVote("v-1", "nonexistent", "g-alice", VoteType.Approve));
        Assert.Contains("Proposal not found", ex.Message);
    }

    [Fact]
    public void CastVote_InvalidGuardian_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.CastVote("v-1", "p-1", "nonexistent", VoteType.Approve));
        Assert.Contains("Guardian not found", ex.Message);
    }

    [Fact]
    public void GetVotes_ReturnsVotesForProposal()
    {
        _engine.CastVote("v-1", "p-1", "g-alice", VoteType.Approve);
        _engine.CastVote("v-2", "p-1", "g-bob", VoteType.Reject);

        var votes = _engine.GetVotes("p-1");

        Assert.Equal(2, votes.Count);
    }

    [Fact]
    public void GetVotes_InvalidProposal_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.GetVotes("nonexistent"));
        Assert.Contains("Proposal not found", ex.Message);
    }

    [Fact]
    public void CountVotes_ReturnsTally()
    {
        _engine.CastVote("v-1", "p-1", "g-alice", VoteType.Approve);
        _engine.CastVote("v-2", "p-1", "g-bob", VoteType.Reject);

        var tally = _engine.CountVotes("p-1");

        Assert.Equal(1, tally.Approve);
        Assert.Equal(1, tally.Reject);
        Assert.Equal(0, tally.Abstain);
        Assert.Equal(2, tally.Total);
    }

    [Fact]
    public void CountVotes_Empty_ReturnsZeros()
    {
        var tally = _engine.CountVotes("p-1");

        Assert.Equal(0, tally.Approve);
        Assert.Equal(0, tally.Reject);
        Assert.Equal(0, tally.Abstain);
        Assert.Equal(0, tally.Total);
    }
}
