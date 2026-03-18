using Whycespace.Engines.T0U.Governance.Proposal.Validation;
using Whycespace.Engines.T0U.Governance.Proposal.Lifecycle;
using Whycespace.Engines.T0U.Governance.Voting.Casting;
using Whycespace.Engines.T0U.Governance.Quorum.Evaluation;
using Whycespace.Engines.T0U.Governance.Delegation.Assignment;
using Whycespace.Engines.T0U.Governance.Dispute.Raising;
using Whycespace.Engines.T0U.Governance.Emergency.Trigger;
using Whycespace.Engines.T0U.Governance.Roles.Assignment;
using Whycespace.Engines.T0U.Governance.Domain.Registration;
using Whycespace.Engines.T0U.Governance.ProposalType.Validation;
using Whycespace.Engines.T0U.Governance.Evidence.Recording;
using Whycespace.Engines.T0U.Governance.Evidence.Audit;
using Whycespace.Engines.T0U.Governance.Workflow.Execution;
using Whycespace.Engines.T0U.Governance.Decisions.Evaluation;
using Whycespace.Engines.T0U.Governance.Guardians.Registry;
using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;
using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;

namespace Whycespace.Governance.Tests;

public class GovernanceProposalRegistryEngineTests
{
    private readonly IdentityRegistry _identityRegistry = new();
    private readonly GuardianRegistryStore _guardianStore = new();
    private readonly GovernanceProposalStore _proposalStore = new();
    private readonly GovernanceProposalRegistryEngine _engine;
    private readonly GuardianRegistryEngine _guardianEngine;

    public GovernanceProposalRegistryEngineTests()
    {
        _engine = new GovernanceProposalRegistryEngine(_proposalStore, _guardianStore);
        _guardianEngine = new GuardianRegistryEngine(_guardianStore, _identityRegistry);

        var identityId = Guid.NewGuid();
        var identity = new IdentityAggregate(IdentityId.From(identityId), IdentityType.User);
        _identityRegistry.Register(identity);
        _guardianEngine.RegisterGuardian("g-alice", identityId, "Alice", new List<string>());
    }

    [Fact]
    public void CreateProposal_Succeeds()
    {
        var proposal = _engine.CreateProposal("p-1", "Fee Reduction", "Reduce transaction fees by 10%", ProposalType.Policy, "g-alice");

        Assert.Equal("p-1", proposal.ProposalId);
        Assert.Equal("Fee Reduction", proposal.Title);
        Assert.Equal("Reduce transaction fees by 10%", proposal.Description);
        Assert.Equal(ProposalType.Policy, proposal.Type);
        Assert.Equal("g-alice", proposal.CreatedBy);
        Assert.Equal(ProposalStatus.Draft, proposal.Status);
    }

    [Fact]
    public void CreateProposal_DuplicateId_Throws()
    {
        _engine.CreateProposal("p-dup", "First", "Desc", ProposalType.Policy, "g-alice");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.CreateProposal("p-dup", "Second", "Desc", ProposalType.Operational, "g-alice"));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public void CreateProposal_EmptyTitle_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.CreateProposal("p-bad", "", "Desc", ProposalType.Policy, "g-alice"));
        Assert.Contains("title is required", ex.Message);
    }

    [Fact]
    public void CreateProposal_InvalidGuardian_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.CreateProposal("p-bad", "Title", "Desc", ProposalType.Policy, "nonexistent"));
        Assert.Contains("Guardian not found", ex.Message);
    }

    [Fact]
    public void GetProposal_Succeeds()
    {
        _engine.CreateProposal("p-get", "Title", "Desc", ProposalType.Constitutional, "g-alice");

        var proposal = _engine.GetProposal("p-get");

        Assert.Equal("p-get", proposal.ProposalId);
    }

    [Fact]
    public void GetProposal_NotFound_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.GetProposal("nonexistent"));
        Assert.Contains("Proposal not found", ex.Message);
    }

    [Fact]
    public void ListProposals_ReturnsAll()
    {
        _engine.CreateProposal("p-a", "First", "Desc", ProposalType.Policy, "g-alice");
        _engine.CreateProposal("p-b", "Second", "Desc", ProposalType.Operational, "g-alice");

        var proposals = _engine.ListProposals();

        Assert.Equal(2, proposals.Count);
    }

    [Fact]
    public void ListProposals_Empty_ReturnsEmpty()
    {
        var proposals = _engine.ListProposals();

        Assert.Empty(proposals);
    }
}
