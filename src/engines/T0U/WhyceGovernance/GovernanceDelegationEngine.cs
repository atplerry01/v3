namespace Whycespace.Engines.T0U.WhyceGovernance;

using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;

public sealed class GovernanceDelegationEngine
{
    private readonly GovernanceDelegationStore _delegationStore;
    private readonly GuardianRegistryStore _guardianStore;
    private readonly GovernanceRoleStore _roleStore;

    public GovernanceDelegationEngine(
        GovernanceDelegationStore delegationStore,
        GuardianRegistryStore guardianStore,
        GovernanceRoleStore roleStore)
    {
        _delegationStore = delegationStore;
        _guardianStore = guardianStore;
        _roleStore = roleStore;
    }

    public GovernanceDelegation CreateDelegation(
        string delegationId,
        string fromGuardian,
        string toGuardian,
        string roleScope,
        DateTime startTime,
        DateTime endTime)
    {
        if (!_guardianStore.Exists(fromGuardian))
            throw new KeyNotFoundException($"Guardian not found: {fromGuardian}");

        if (!_guardianStore.Exists(toGuardian))
            throw new KeyNotFoundException($"Guardian not found: {toGuardian}");

        if (fromGuardian == toGuardian)
            throw new InvalidOperationException("Cannot delegate to self.");

        if (endTime <= startTime)
            throw new InvalidOperationException("Delegation must have a valid expiration (EndTime must be after StartTime).");

        if (!_roleStore.RoleExists(roleScope))
            throw new KeyNotFoundException($"Role not found: {roleScope}");

        var fromRoles = _roleStore.GetGuardianRoleIds(fromGuardian);
        if (!fromRoles.Contains(roleScope))
            throw new InvalidOperationException($"Guardian '{fromGuardian}' does not hold role '{roleScope}' and cannot delegate it.");

        var delegation = new GovernanceDelegation(
            delegationId,
            fromGuardian,
            toGuardian,
            roleScope,
            startTime,
            endTime,
            DelegationStatus.Active);

        _delegationStore.Add(delegation);
        return delegation;
    }

    public GovernanceDelegation RevokeDelegation(string delegationId)
    {
        var delegation = _delegationStore.Get(delegationId)
            ?? throw new KeyNotFoundException($"Delegation not found: {delegationId}");

        if (delegation.Status == DelegationStatus.Revoked)
            throw new InvalidOperationException($"Delegation already revoked: {delegationId}");

        var revoked = delegation with { Status = DelegationStatus.Revoked };
        _delegationStore.Update(revoked);
        return revoked;
    }

    public IReadOnlyList<GovernanceDelegation> GetDelegations(string guardianId)
    {
        if (!_guardianStore.Exists(guardianId))
            throw new KeyNotFoundException($"Guardian not found: {guardianId}");

        return _delegationStore.GetByGuardian(guardianId);
    }
}
