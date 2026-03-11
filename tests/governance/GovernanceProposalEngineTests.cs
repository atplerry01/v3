using Whycespace.Engines.T0U.Governance;
using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;
using Whycespace.System.WhyceID.Aggregates;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;

namespace Whycespace.Governance.Tests;

public class GovernanceProposalEngineTests
{
    private readonly GovernanceProposalStore _proposalStore = new();
    private readonly GuardianRegistryStore _guardianStore = new();
    private readonly GovernanceProposalEngine _engine;
    private readonly GovernanceProposalRegistryEngine _registryEngine;

    public GovernanceProposalEngineTests()
    {
        _engine = new GovernanceProposalEngine(_proposalStore);
        _registryEngine = new GovernanceProposalRegistryEngine(_proposalStore, _guardianStore);

        var identityRegistry = new IdentityRegistry();
        var identityId = Guid.NewGuid();
        identityRegistry.Register(new IdentityAggregate(IdentityId.From(identityId), IdentityType.User));
        var guardianEngine = new GuardianRegistryEngine(_guardianStore, identityRegistry);
        guardianEngine.RegisterGuardian("g-alice", identityId, "Alice", new List<string>());
    }

    private GovernanceProposal CreateDraftProposal(string id = "p-1")
    {
        return _registryEngine.CreateProposal(id, "Test Proposal", "Description", ProposalType.Policy, "g-alice");
    }

    [Fact]
    public void OpenProposal_FromDraft_Succeeds()
    {
        CreateDraftProposal();

        var result = _engine.OpenProposal("p-1");

        Assert.Equal(ProposalStatus.Open, result.Status);
    }

    [Fact]
    public void OpenProposal_NotDraft_Throws()
    {
        CreateDraftProposal();
        _engine.OpenProposal("p-1");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.OpenProposal("p-1"));
        Assert.Contains("must be in Draft", ex.Message);
    }

    [Fact]
    public void StartVoting_FromOpen_Succeeds()
    {
        CreateDraftProposal();
        _engine.OpenProposal("p-1");

        var result = _engine.StartVoting("p-1");

        Assert.Equal(ProposalStatus.Voting, result.Status);
    }

    [Fact]
    public void StartVoting_NotOpen_Throws()
    {
        CreateDraftProposal();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.StartVoting("p-1"));
        Assert.Contains("must be Open", ex.Message);
    }

    [Fact]
    public void RejectProposal_FromVoting_Succeeds()
    {
        CreateDraftProposal();
        _engine.OpenProposal("p-1");
        _engine.StartVoting("p-1");

        var result = _engine.RejectProposal("p-1");

        Assert.Equal(ProposalStatus.Rejected, result.Status);
    }

    [Fact]
    public void RejectProposal_NotVoting_Throws()
    {
        CreateDraftProposal();
        _engine.OpenProposal("p-1");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.RejectProposal("p-1"));
        Assert.Contains("must be in Voting", ex.Message);
    }

    [Fact]
    public void CloseProposal_FromOpen_Succeeds()
    {
        CreateDraftProposal();
        _engine.OpenProposal("p-1");

        var result = _engine.CloseProposal("p-1");

        Assert.Equal(ProposalStatus.Closed, result.Status);
    }

    [Fact]
    public void CloseProposal_FromVoting_Succeeds()
    {
        CreateDraftProposal();
        _engine.OpenProposal("p-1");
        _engine.StartVoting("p-1");

        var result = _engine.CloseProposal("p-1");

        Assert.Equal(ProposalStatus.Closed, result.Status);
    }

    [Fact]
    public void CloseProposal_AlreadyClosed_Throws()
    {
        CreateDraftProposal();
        _engine.OpenProposal("p-1");
        _engine.CloseProposal("p-1");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.CloseProposal("p-1"));
        Assert.Contains("already closed", ex.Message);
    }

    [Fact]
    public void CloseProposal_FromDraft_Throws()
    {
        CreateDraftProposal();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.CloseProposal("p-1"));
        Assert.Contains("Cannot close a Draft", ex.Message);
    }

    [Fact]
    public void NotFound_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.OpenProposal("nonexistent"));
        Assert.Contains("Proposal not found", ex.Message);
    }
}
