namespace Whycespace.Engines.T0U.Governance.Roles.Assignment;

using Whycespace.Domain.Governance.Events;
using Whycespace.Engines.T0U.Governance.Roles.Revocation;

using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;

public sealed class GovernanceRoleEngine
{
    private readonly GovernanceRoleStore _roleStore;
    private readonly GuardianRegistryStore _guardianStore;

    private static readonly Dictionary<GuardianRole, int> RoleHierarchyLevel = new()
    {
        [GuardianRole.ConstitutionGuardian] = 4,
        [GuardianRole.SeniorGuardian] = 3,
        [GuardianRole.Guardian] = 2,
        [GuardianRole.DomainGuardian] = 1,
        [GuardianRole.EmergencyGuardian] = 0,
    };

    public GovernanceRoleEngine(
        GovernanceRoleStore roleStore,
        GuardianRegistryStore guardianStore)
    {
        _roleStore = roleStore;
        _guardianStore = guardianStore;
    }

    public (GovernanceRoleResult Result, GovernanceRoleAssignedEvent? Event) Execute(AssignGovernanceRoleCommand command)
    {
        var guardianId = command.TargetGuardianId.ToString();

        if (!_guardianStore.Exists(guardianId))
            return (GovernanceRoleResult.Failure(command.TargetGuardianId, command.AssignedRole,
                GovernanceRoleAction.Assigned, $"Guardian not found: {guardianId}"), null);

        if (!Enum.IsDefined(command.AssignedRole))
            return (GovernanceRoleResult.Failure(command.TargetGuardianId, command.AssignedRole,
                GovernanceRoleAction.Assigned, $"Invalid role: {command.AssignedRole}"), null);

        if (string.IsNullOrWhiteSpace(command.AuthorityDomain))
            return (GovernanceRoleResult.Failure(command.TargetGuardianId, command.AssignedRole,
                GovernanceRoleAction.Assigned, "Authority domain must not be empty."), null);

        var requesterId = command.RequestedBy.ToString();
        if (!ValidateHierarchy(requesterId, command.AssignedRole))
            return (GovernanceRoleResult.Failure(command.TargetGuardianId, command.AssignedRole,
                GovernanceRoleAction.Assigned,
                $"Requester does not have sufficient authority to assign role {command.AssignedRole}."), null);

        var roleId = command.AssignedRole.ToString();
        var existingRoleIds = _roleStore.GetGuardianRoleIds(guardianId);
        if (existingRoleIds.Contains(roleId))
            return (GovernanceRoleResult.Failure(command.TargetGuardianId, command.AssignedRole,
                GovernanceRoleAction.Assigned,
                $"Guardian already has role {command.AssignedRole}."), null);

        var result = new GovernanceRoleResult(
            true, command.TargetGuardianId, command.AssignedRole,
            GovernanceRoleAction.Assigned, "Role assigned successfully.", DateTime.UtcNow);

        var domainEvent = new GovernanceRoleAssignedEvent(
            Guid.NewGuid(), command.TargetGuardianId, command.AssignedRole.ToString(),
            command.AuthorityDomain, command.RequestedBy, command.Timestamp);

        return (result, domainEvent);
    }

    public (GovernanceRoleResult Result, GovernanceRoleRevokedEvent? Event) Execute(RevokeGovernanceRoleCommand command)
    {
        var guardianId = command.TargetGuardianId.ToString();

        if (!_guardianStore.Exists(guardianId))
            return (GovernanceRoleResult.Failure(command.TargetGuardianId, command.RevokedRole,
                GovernanceRoleAction.Revoked, $"Guardian not found: {guardianId}"), null);

        if (!Enum.IsDefined(command.RevokedRole))
            return (GovernanceRoleResult.Failure(command.TargetGuardianId, command.RevokedRole,
                GovernanceRoleAction.Revoked, $"Invalid role: {command.RevokedRole}"), null);

        var requesterId = command.RequestedBy.ToString();
        if (!ValidateHierarchy(requesterId, command.RevokedRole))
            return (GovernanceRoleResult.Failure(command.TargetGuardianId, command.RevokedRole,
                GovernanceRoleAction.Revoked,
                $"Requester does not have sufficient authority to revoke role {command.RevokedRole}."), null);

        var roleId = command.RevokedRole.ToString();
        var existingRoleIds = _roleStore.GetGuardianRoleIds(guardianId);
        if (!existingRoleIds.Contains(roleId))
            return (GovernanceRoleResult.Failure(command.TargetGuardianId, command.RevokedRole,
                GovernanceRoleAction.Revoked,
                $"Guardian does not have role {command.RevokedRole}."), null);

        var result = new GovernanceRoleResult(
            true, command.TargetGuardianId, command.RevokedRole,
            GovernanceRoleAction.Revoked, "Role revoked successfully.", DateTime.UtcNow);

        var domainEvent = new GovernanceRoleRevokedEvent(
            Guid.NewGuid(), command.TargetGuardianId, command.RevokedRole.ToString(),
            command.RequestedBy, command.Reason, command.Timestamp);

        return (result, domainEvent);
    }

    public static bool CanAssignRole(GuardianRole requesterHighestRole, GuardianRole targetRole)
    {
        var requesterLevel = RoleHierarchyLevel.GetValueOrDefault(requesterHighestRole, -1);
        var targetLevel = RoleHierarchyLevel.GetValueOrDefault(targetRole, -1);
        return requesterLevel > targetLevel;
    }

    private bool ValidateHierarchy(string requesterId, GuardianRole targetRole)
    {
        var requesterRoleIds = _roleStore.GetGuardianRoleIds(requesterId);
        if (requesterRoleIds.Count == 0)
            return false;

        var highestLevel = -1;
        foreach (var roleId in requesterRoleIds)
        {
            if (Enum.TryParse<GuardianRole>(roleId, out var parsed))
            {
                var level = RoleHierarchyLevel.GetValueOrDefault(parsed, -1);
                if (level > highestLevel)
                    highestLevel = level;
            }
        }

        var targetLevel = RoleHierarchyLevel.GetValueOrDefault(targetRole, -1);
        return highestLevel > targetLevel;
    }
}
