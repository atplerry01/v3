using Whycespace.Engines.T0U.Governance;
using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;
using Whycespace.System.WhyceID.Aggregates;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;

namespace Whycespace.Governance.Tests;

public class GovernanceDomainScopeEngineTests
{
    private readonly GovernanceDomainScopeStore _scopeStore = new();
    private readonly GovernanceProposalStore _proposalStore = new();
    private readonly GuardianRegistryStore _guardianStore = new();
    private readonly GovernanceDomainScopeEngine _engine;
    private readonly GovernanceProposalRegistryEngine _registryEngine;

    public GovernanceDomainScopeEngineTests()
    {
        _engine = new GovernanceDomainScopeEngine(_scopeStore, _proposalStore);
        _registryEngine = new GovernanceProposalRegistryEngine(_proposalStore, _guardianStore);

        var identityRegistry = new IdentityRegistry();
        var identityId = Guid.NewGuid();
        identityRegistry.Register(new IdentityAggregate(IdentityId.From(identityId), IdentityType.User));
        var guardianEngine = new GuardianRegistryEngine(_guardianStore, identityRegistry);
        guardianEngine.RegisterGuardian("g-alice", identityId, "Alice", new List<string>());

        _scopeStore.AddScope(new GovernanceDomainScope("policy", "Policy", "Policy domain"));
        _scopeStore.AddScope(new GovernanceDomainScope("cluster", "Cluster", "Cluster domain"));
        _scopeStore.AddScope(new GovernanceDomainScope("spv", "SPV", "SPV domain"));
        _scopeStore.AddScope(new GovernanceDomainScope("identity", "Identity", "Identity domain"));
        _scopeStore.AddScope(new GovernanceDomainScope("finance", "Finance", "Finance domain"));
    }

    [Fact]
    public void AssignScope_Succeeds()
    {
        _registryEngine.CreateProposal("p-1", "Title", "Desc", ProposalType.Policy, "g-alice");

        _engine.AssignScope("p-1", "policy");

        var scope = _engine.GetScope("p-1");
        Assert.Equal("policy", scope.ScopeId);
        Assert.Equal("Policy", scope.Name);
    }

    [Fact]
    public void AssignScope_InvalidProposal_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.AssignScope("nonexistent", "policy"));
        Assert.Contains("Proposal not found", ex.Message);
    }

    [Fact]
    public void AssignScope_InvalidScope_Throws()
    {
        _registryEngine.CreateProposal("p-2", "Title", "Desc", ProposalType.Policy, "g-alice");

        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.AssignScope("p-2", "nonexistent"));
        Assert.Contains("Domain scope not found", ex.Message);
    }

    [Fact]
    public void AssignScope_AlreadyAssigned_Throws()
    {
        _registryEngine.CreateProposal("p-3", "Title", "Desc", ProposalType.Policy, "g-alice");
        _engine.AssignScope("p-3", "policy");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.AssignScope("p-3", "cluster"));
        Assert.Contains("already has a scope", ex.Message);
    }

    [Fact]
    public void GetScope_NoScopeAssigned_Throws()
    {
        _registryEngine.CreateProposal("p-4", "Title", "Desc", ProposalType.Policy, "g-alice");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.GetScope("p-4"));
        Assert.Contains("No scope assigned", ex.Message);
    }

    [Fact]
    public void GetScope_InvalidProposal_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.GetScope("nonexistent"));
        Assert.Contains("Proposal not found", ex.Message);
    }
}
