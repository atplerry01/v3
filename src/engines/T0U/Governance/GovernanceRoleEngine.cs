namespace Whycespace.Engines.T0U.Governance;

using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;

public sealed class GovernanceRoleEngine
{
    private readonly GovernanceRoleStore _roleStore;
    private readonly GuardianRegistryStore _guardianStore;

    public GovernanceRoleEngine(
        GovernanceRoleStore roleStore,
        GuardianRegistryStore guardianStore)
    {
        _roleStore = roleStore;
        _guardianStore = guardianStore;
    }

    public GovernanceRole CreateRole(string roleId, string name, string description, IReadOnlyList<string> permissions)
    {
        var role = new GovernanceRole(roleId, name, description, permissions);
        _roleStore.AddRole(role);
        return role;
    }

    public void AssignRole(string guardianId, string roleId)
    {
        if (!_guardianStore.Exists(guardianId))
            throw new KeyNotFoundException($"Guardian not found: {guardianId}");

        if (!_roleStore.RoleExists(roleId))
            throw new KeyNotFoundException($"Role not found: {roleId}");

        _roleStore.AssignRole(guardianId, roleId);
    }

    public void RevokeRole(string guardianId, string roleId)
    {
        if (!_guardianStore.Exists(guardianId))
            throw new KeyNotFoundException($"Guardian not found: {guardianId}");

        _roleStore.RevokeRole(guardianId, roleId);
    }

    public IReadOnlyList<GovernanceRole> GetGuardianRoles(string guardianId)
    {
        var roleIds = _roleStore.GetGuardianRoleIds(guardianId);
        var roles = new List<GovernanceRole>();

        foreach (var roleId in roleIds)
        {
            var role = _roleStore.GetRole(roleId);
            if (role is not null)
                roles.Add(role);
        }

        return roles;
    }
}
