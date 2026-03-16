namespace Whycespace.Systems.Upstream.Governance.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Upstream.Governance.Models;

public sealed class GovernanceDomainScopeStore
{
    private readonly ConcurrentDictionary<string, GovernanceDomainScope> _scopes = new();
    private readonly ConcurrentDictionary<string, string> _proposalScopes = new();

    public void AddScope(GovernanceDomainScope scope)
    {
        if (!_scopes.TryAdd(scope.ScopeId, scope))
            throw new InvalidOperationException($"Domain scope already exists: {scope.ScopeId}");
    }

    public GovernanceDomainScope? GetScope(string scopeId)
    {
        _scopes.TryGetValue(scopeId, out var scope);
        return scope;
    }

    public bool ScopeExists(string scopeId)
    {
        return _scopes.ContainsKey(scopeId);
    }

    public void DeactivateScope(string scopeId, DateTime deactivatedAt)
    {
        if (!_scopes.TryGetValue(scopeId, out var scope))
            throw new KeyNotFoundException($"Domain scope not found: {scopeId}");

        _scopes[scopeId] = scope with { IsActive = false, DeactivatedAt = deactivatedAt };
    }

    public IReadOnlyList<GovernanceDomainScope> ListAll()
    {
        return _scopes.Values.ToList();
    }

    public void AssignProposalScope(string proposalId, string scopeId)
    {
        if (!_proposalScopes.TryAdd(proposalId, scopeId))
            throw new InvalidOperationException($"Proposal already has a scope assigned: {proposalId}");
    }

    public string? GetProposalScope(string proposalId)
    {
        _proposalScopes.TryGetValue(proposalId, out var scopeId);
        return scopeId;
    }
}
