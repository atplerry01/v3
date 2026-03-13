namespace Whycespace.Engines.T0U.Governance;

using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;

public sealed class GovernanceDomainScopeEngine
{
    private readonly GovernanceDomainScopeStore _scopeStore;
    private readonly GovernanceProposalStore _proposalStore;

    public GovernanceDomainScopeEngine(
        GovernanceDomainScopeStore scopeStore,
        GovernanceProposalStore proposalStore)
    {
        _scopeStore = scopeStore;
        _proposalStore = proposalStore;
    }

    public void AssignScope(string proposalId, string scopeId)
    {
        if (_proposalStore.Get(proposalId) is null)
            throw new KeyNotFoundException($"Proposal not found: {proposalId}");

        if (!_scopeStore.ScopeExists(scopeId))
            throw new KeyNotFoundException($"Domain scope not found: {scopeId}");

        _scopeStore.AssignProposalScope(proposalId, scopeId);
    }

    public GovernanceDomainScope GetScope(string proposalId)
    {
        if (_proposalStore.Get(proposalId) is null)
            throw new KeyNotFoundException($"Proposal not found: {proposalId}");

        var scopeId = _scopeStore.GetProposalScope(proposalId)
            ?? throw new InvalidOperationException($"No scope assigned to proposal: {proposalId}");

        return _scopeStore.GetScope(scopeId)
            ?? throw new KeyNotFoundException($"Domain scope not found: {scopeId}");
    }
}
