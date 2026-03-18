namespace Whycespace.Engines.T0U.WhycePolicy.Enforcement.Authority;

using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

public sealed class GovernanceAuthorityEngine
{
    private readonly GovernanceAuthorityStore _store;

    public GovernanceAuthorityEngine(GovernanceAuthorityStore store)
    {
        _store = store;
    }

    public GovernanceAuthorityRecord AssignAuthority(string actorId, GovernanceRole role)
    {
        var record = new GovernanceAuthorityRecord(actorId, role, DateTime.UtcNow);
        _store.AssignRole(record);
        return record;
    }

    public bool HasAuthority(string actorId, GovernanceRole role)
    {
        if (_store.HasRole(actorId, GovernanceRole.PolicyAdministrator))
            return true;

        return _store.HasRole(actorId, role);
    }

    public void ValidateAuthority(string actorId, GovernanceRole requiredRole)
    {
        if (!HasAuthority(actorId, requiredRole))
            throw new UnauthorizedAccessException(
                $"Actor '{actorId}' lacks required governance role '{requiredRole}'.");
    }

    public IReadOnlyList<GovernanceAuthorityRecord> GetRoles(string actorId)
    {
        return _store.GetRoles(actorId);
    }
}
