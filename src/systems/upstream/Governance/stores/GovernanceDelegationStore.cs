namespace Whycespace.Systems.Upstream.Governance.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Upstream.Governance.Models;

public sealed class GovernanceDelegationStore
{
    private readonly ConcurrentDictionary<string, GovernanceDelegation> _delegations = new();

    public void Add(GovernanceDelegation delegation)
    {
        if (!_delegations.TryAdd(delegation.DelegationId, delegation))
            throw new InvalidOperationException($"Delegation already exists: {delegation.DelegationId}");
    }

    public GovernanceDelegation? Get(string delegationId)
    {
        _delegations.TryGetValue(delegationId, out var delegation);
        return delegation;
    }

    public void Update(GovernanceDelegation delegation)
    {
        if (!_delegations.ContainsKey(delegation.DelegationId))
            throw new KeyNotFoundException($"Delegation not found: {delegation.DelegationId}");

        _delegations[delegation.DelegationId] = delegation;
    }

    public IReadOnlyList<GovernanceDelegation> GetByGuardian(string guardianId)
    {
        return _delegations.Values
            .Where(d => d.FromGuardian == guardianId || d.ToGuardian == guardianId)
            .ToList();
    }

    public IReadOnlyList<GovernanceDelegation> GetActiveDelegations(string guardianId, DateTime now)
    {
        return _delegations.Values
            .Where(d => d.ToGuardian == guardianId
                && d.Status == DelegationStatus.Active
                && d.StartTime <= now
                && d.EndTime > now)
            .ToList();
    }
}
