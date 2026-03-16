namespace Whycespace.Systems.Upstream.Governance.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Upstream.Governance.Models;

public sealed class GovernanceRoleStore
{
    private readonly ConcurrentDictionary<string, GovernanceRole> _roles = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _guardianRoles = new();

    public void AddRole(GovernanceRole role)
    {
        if (!_roles.TryAdd(role.RoleId, role))
            throw new InvalidOperationException($"Role already exists: {role.RoleId}");
    }

    public GovernanceRole? GetRole(string roleId)
    {
        _roles.TryGetValue(roleId, out var role);
        return role;
    }

    public IReadOnlyList<GovernanceRole> ListRoles()
    {
        return _roles.Values.ToList();
    }

    public bool RoleExists(string roleId)
    {
        return _roles.ContainsKey(roleId);
    }

    public void AssignRole(string guardianId, string roleId)
    {
        var set = _guardianRoles.GetOrAdd(guardianId, _ => new HashSet<string>());

        lock (set)
        {
            if (!set.Add(roleId))
                throw new InvalidOperationException($"Role '{roleId}' already assigned to guardian '{guardianId}'.");
        }
    }

    public void RevokeRole(string guardianId, string roleId)
    {
        if (!_guardianRoles.TryGetValue(guardianId, out var set))
            throw new KeyNotFoundException($"No roles found for guardian: {guardianId}");

        lock (set)
        {
            if (!set.Remove(roleId))
                throw new InvalidOperationException($"Guardian '{guardianId}' does not have role '{roleId}'.");
        }
    }

    public IReadOnlyList<string> GetGuardianRoleIds(string guardianId)
    {
        if (_guardianRoles.TryGetValue(guardianId, out var set))
        {
            lock (set)
            {
                return set.ToList();
            }
        }

        return Array.Empty<string>();
    }
}
