using Whycespace.Engines.T0U.Governance;
using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;
using Whycespace.System.WhyceID.Aggregates;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;

namespace Whycespace.Governance.Tests;

public class QuorumEngineTests
{
    private readonly IdentityRegistry _identityRegistry = new();
    private readonly GuardianRegistryStore _guardianStore = new();
    private readonly GovernanceProposalStore _proposalStore = new();
    private readonly GovernanceVoteStore _voteStore = new();
    private readonly GuardianRegistryEngine _guardianEngine;
    private readonly GovernanceProposalRegistryEngine _registryEngine;
    private readonly GovernanceProposalEngine _proposalEngine;
    private readonly VotingEngine _votingEngine;
    private readonly Guid _identityId;

    public QuorumEngineTests()
    {
        _guardianEngine = new GuardianRegistryEngine(_guardianStore, _identityRegistry);
        _registryEngine = new GovernanceProposalRegistryEngine(_proposalStore, _guardianStore);
        _proposalEngine = new GovernanceProposalEngine(_proposalStore);
        _votingEngine = new VotingEngine(_voteStore, _proposalStore, _guardianStore);

        _identityId = Guid.NewGuid();
        _identityRegistry.Register(new IdentityAggregate(IdentityId.From(_identityId), IdentityType.User));
    }

    private string RegisterActiveGuardian(string id, string name)
    {
        _guardianEngine.RegisterGuardian(id, _identityId, name, new List<string>());
        _guardianEngine.ActivateGuardian(id);
        return id;
    }

    private string CreateVotingProposal(string id)
    {
        _registryEngine.CreateProposal(id, "Proposal", "Desc", ProposalType.Policy, RegisterActiveGuardian($"g-creator-{id}", "Creator"));
        _proposalEngine.OpenProposal(id);
        _proposalEngine.StartVoting(id);
        return id;
    }

    [Fact]
    public void CalculateQuorumThreshold_60Percent_Of5_Returns3()
    {
        for (var i = 0; i < 5; i++)
            RegisterActiveGuardian($"g-{i}", $"Guardian{i}");

        var engine = new QuorumEngine(_voteStore, _guardianStore, new QuorumConfig(60));

        Assert.Equal(3, engine.CalculateQuorumThreshold());
    }

    [Fact]
    public void CalculateQuorumThreshold_RoundsUp()
    {
        for (var i = 0; i < 3; i++)
            RegisterActiveGuardian($"g-{i}", $"Guardian{i}");

        var engine = new QuorumEngine(_voteStore, _guardianStore, new QuorumConfig(60));

        // 60% of 3 = 1.8, ceiling = 2
        Assert.Equal(2, engine.CalculateQuorumThreshold());
    }

    [Fact]
    public void CalculateQuorumThreshold_NoActiveGuardians_ReturnsZero()
    {
        var engine = new QuorumEngine(_voteStore, _guardianStore, new QuorumConfig(60));

        Assert.Equal(0, engine.CalculateQuorumThreshold());
    }

    [Fact]
    public void CalculateQuorumThreshold_ExcludesInactive()
    {
        RegisterActiveGuardian("g-active-1", "Active1");
        RegisterActiveGuardian("g-active-2", "Active2");
        _guardianEngine.RegisterGuardian("g-inactive", _identityId, "Inactive", new List<string>());

        var engine = new QuorumEngine(_voteStore, _guardianStore, new QuorumConfig(60));

        // 60% of 2 active = 1.2, ceiling = 2
        Assert.Equal(2, engine.CalculateQuorumThreshold());
    }

    [Fact]
    public void CheckQuorum_Met_ReturnsTrue()
    {
        var g1 = RegisterActiveGuardian("g-q1", "G1");
        var g2 = RegisterActiveGuardian("g-q2", "G2");
        RegisterActiveGuardian("g-q3", "G3");

        var proposalId = CreateVotingProposal("p-quorum");

        _votingEngine.CastVote("v-1", proposalId, g1, VoteType.Approve);
        _votingEngine.CastVote("v-2", proposalId, g2, VoteType.Reject);

        // threshold for active guardians depends on total active count
        // We have 3 original + 1 creator = at least 4 active. Let's use a low threshold.
        var engine = new QuorumEngine(_voteStore, _guardianStore, new QuorumConfig(30));

        Assert.True(engine.CheckQuorum(proposalId));
    }

    [Fact]
    public void CheckQuorum_NotMet_ReturnsFalse()
    {
        RegisterActiveGuardian("g-nm1", "G1");
        RegisterActiveGuardian("g-nm2", "G2");
        RegisterActiveGuardian("g-nm3", "G3");
        RegisterActiveGuardian("g-nm4", "G4");
        RegisterActiveGuardian("g-nm5", "G5");

        var proposalId = CreateVotingProposal("p-noquorum");

        _votingEngine.CastVote("v-1", proposalId, "g-nm1", VoteType.Approve);

        var engine = new QuorumEngine(_voteStore, _guardianStore, new QuorumConfig(60));

        Assert.False(engine.CheckQuorum(proposalId));
    }

    [Fact]
    public void CheckQuorum_ConfigurableThreshold()
    {
        var g1 = RegisterActiveGuardian("g-ct1", "G1");
        RegisterActiveGuardian("g-ct2", "G2");

        var proposalId = CreateVotingProposal("p-config");

        _votingEngine.CastVote("v-1", proposalId, g1, VoteType.Approve);

        // 50% of 3 active (2 + creator) = 1.5, ceiling = 2. 1 vote < 2 = false
        var strict = new QuorumEngine(_voteStore, _guardianStore, new QuorumConfig(50));
        // 25% of 3 active = 0.75, ceiling = 1. 1 vote >= 1 = true
        var lenient = new QuorumEngine(_voteStore, _guardianStore, new QuorumConfig(25));

        Assert.False(strict.CheckQuorum(proposalId));
        Assert.True(lenient.CheckQuorum(proposalId));
    }
}
