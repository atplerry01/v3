using Whycespace.Contracts.Runtime;
using Whycespace.Engines.T0U.WhyceGovernance;
using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;
using Whycespace.Systems.WhyceID.Registry;

namespace Whycespace.Platform.Dispatch.Handlers;

public sealed class GovernanceCommandHandler
{
    private readonly GuardianRegistryStore _guardianRegistryStore;
    private readonly GovernanceRoleStore _governanceRoleStore;
    private readonly GovernanceDelegationStore _delegationStore;
    private readonly IdentityRegistry _identityRegistry;

    public GovernanceCommandHandler(
        GuardianRegistryStore guardianRegistryStore,
        GovernanceRoleStore governanceRoleStore,
        GovernanceDelegationStore delegationStore,
        IdentityRegistry identityRegistry)
    {
        _guardianRegistryStore = guardianRegistryStore;
        _governanceRoleStore = governanceRoleStore;
        _delegationStore = delegationStore;
        _identityRegistry = identityRegistry;
    }

    public bool CanHandle(string command) => command.StartsWith("governance.");

    public Task<DispatchResult> HandleAsync(string command, Dictionary<string, object> payload)
    {
        return command switch
        {
            "governance.guardian.register" => Task.FromResult(HandleGuardianRegister(payload)),
            "governance.guardian.activate" => Task.FromResult(HandleGuardianActivate(payload)),
            "governance.guardian.deactivate" => Task.FromResult(HandleGuardianDeactivate(payload)),
            "governance.role.create" => Task.FromResult(HandleRoleCreate(payload)),
            "governance.role.assign" => Task.FromResult(HandleRoleAssign(payload)),
            "governance.role.revoke" => Task.FromResult(HandleRoleRevoke(payload)),
            "governance.role.getGuardianRoles" => Task.FromResult(HandleGetGuardianRoles(payload)),
            "governance.delegation.create" => Task.FromResult(HandleDelegationCreate(payload)),
            "governance.delegation.revoke" => Task.FromResult(HandleDelegationRevoke(payload)),
            "governance.delegation.get" => Task.FromResult(HandleDelegationGet(payload)),
            "governance.delegation.listByGuardian" => Task.FromResult(HandleDelegationListByGuardian(payload)),
            _ => Task.FromResult(DispatchResult.Fail($"Unknown governance command: {command}"))
        };
    }

    private DispatchResult HandleGuardianRegister(Dictionary<string, object> payload)
    {
        var engine = new GuardianRegistryEngine(_guardianRegistryStore, _identityRegistry);

        var guardianId = (string)payload["guardianId"];
        var identityId = (Guid)payload["identityId"];
        var name = (string)payload["name"];
        var roles = (List<string>)payload["roles"];

        var guardian = engine.RegisterGuardian(guardianId, identityId, name, roles);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["GuardianId"] = guardian.GuardianId,
            ["IdentityId"] = guardian.IdentityId,
            ["Name"] = guardian.Name,
            ["Status"] = guardian.Status,
            ["Roles"] = guardian.Roles,
            ["CreatedAt"] = guardian.CreatedAt,
            ["ActivatedAt"] = guardian.ActivatedAt!
        });
    }

    private DispatchResult HandleGuardianActivate(Dictionary<string, object> payload)
    {
        var engine = new GuardianRegistryEngine(_guardianRegistryStore, _identityRegistry);

        var guardianId = (string)payload["guardianId"];
        var guardian = engine.ActivateGuardian(guardianId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["GuardianId"] = guardian.GuardianId,
            ["IdentityId"] = guardian.IdentityId,
            ["Name"] = guardian.Name,
            ["Status"] = guardian.Status,
            ["Roles"] = guardian.Roles,
            ["CreatedAt"] = guardian.CreatedAt,
            ["ActivatedAt"] = guardian.ActivatedAt!
        });
    }

    private DispatchResult HandleGuardianDeactivate(Dictionary<string, object> payload)
    {
        var engine = new GuardianRegistryEngine(_guardianRegistryStore, _identityRegistry);

        var guardianId = (string)payload["guardianId"];
        var guardian = engine.DeactivateGuardian(guardianId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["GuardianId"] = guardian.GuardianId,
            ["IdentityId"] = guardian.IdentityId,
            ["Name"] = guardian.Name,
            ["Status"] = guardian.Status,
            ["Roles"] = guardian.Roles,
            ["CreatedAt"] = guardian.CreatedAt,
            ["ActivatedAt"] = guardian.ActivatedAt!
        });
    }

    private DispatchResult HandleRoleCreate(Dictionary<string, object> payload)
    {
        var roleId = (string)payload["roleId"];
        var name = (string)payload["name"];
        var description = (string)payload["description"];
        var permissions = (List<string>)payload["permissions"];

        var role = new GovernanceRole(roleId, name, description, permissions);
        _governanceRoleStore.AddRole(role);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["RoleId"] = role.RoleId,
            ["Name"] = role.Name,
            ["Description"] = role.Description,
            ["Permissions"] = role.Permissions
        });
    }

    private DispatchResult HandleRoleAssign(Dictionary<string, object> payload)
    {
        var guardianId = (string)payload["guardianId"];
        var roleId = (string)payload["roleId"];

        _governanceRoleStore.AssignRole(guardianId, roleId);
        var roleIds = _governanceRoleStore.GetGuardianRoleIds(guardianId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["guardianId"] = guardianId,
            ["roles"] = roleIds.Select(id =>
            {
                var r = _governanceRoleStore.GetRole(id);
                return new Dictionary<string, object>
                {
                    ["RoleId"] = r?.RoleId ?? id,
                    ["Name"] = r?.Name ?? id,
                    ["Description"] = r?.Description ?? "",
                    ["Permissions"] = (object)(r?.Permissions ?? (IReadOnlyList<string>)Array.Empty<string>())
                };
            }).ToList()
        });
    }

    private DispatchResult HandleRoleRevoke(Dictionary<string, object> payload)
    {
        var guardianId = (string)payload["guardianId"];
        var roleId = (string)payload["roleId"];

        _governanceRoleStore.RevokeRole(guardianId, roleId);
        var roleIds = _governanceRoleStore.GetGuardianRoleIds(guardianId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["guardianId"] = guardianId,
            ["roles"] = roleIds.Select(id =>
            {
                var r = _governanceRoleStore.GetRole(id);
                return new Dictionary<string, object>
                {
                    ["RoleId"] = r?.RoleId ?? id,
                    ["Name"] = r?.Name ?? id,
                    ["Description"] = r?.Description ?? "",
                    ["Permissions"] = (object)(r?.Permissions ?? (IReadOnlyList<string>)Array.Empty<string>())
                };
            }).ToList()
        });
    }

    private DispatchResult HandleGetGuardianRoles(Dictionary<string, object> payload)
    {
        var guardianId = (string)payload["guardianId"];
        var roleIds = _governanceRoleStore.GetGuardianRoleIds(guardianId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["guardianId"] = guardianId,
            ["roles"] = roleIds.Select(id =>
            {
                var r = _governanceRoleStore.GetRole(id);
                return new Dictionary<string, object>
                {
                    ["RoleId"] = r?.RoleId ?? id,
                    ["Name"] = r?.Name ?? id,
                    ["Description"] = r?.Description ?? "",
                    ["Permissions"] = (object)(r?.Permissions ?? (IReadOnlyList<string>)Array.Empty<string>())
                };
            }).ToList()
        });
    }

    private DispatchResult HandleDelegationCreate(Dictionary<string, object> payload)
    {
        var engine = new GovernanceDelegationEngine(_delegationStore, _guardianRegistryStore, _governanceRoleStore);

        var delegationId = (string)payload["delegationId"];
        var fromGuardian = (string)payload["fromGuardian"];
        var toGuardian = (string)payload["toGuardian"];
        var roleScope = (string)payload["roleScope"];
        var startTime = (DateTime)payload["startTime"];
        var endTime = (DateTime)payload["endTime"];

        var delegation = engine.CreateDelegation(delegationId, fromGuardian, toGuardian, roleScope, startTime, endTime);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["DelegationId"] = delegation.DelegationId,
            ["FromGuardian"] = delegation.FromGuardian,
            ["ToGuardian"] = delegation.ToGuardian,
            ["RoleScope"] = delegation.RoleScope,
            ["StartTime"] = delegation.StartTime,
            ["EndTime"] = delegation.EndTime,
            ["Status"] = delegation.Status.ToString()
        });
    }

    private DispatchResult HandleDelegationRevoke(Dictionary<string, object> payload)
    {
        var engine = new GovernanceDelegationEngine(_delegationStore, _guardianRegistryStore, _governanceRoleStore);

        var delegationId = (string)payload["delegationId"];
        var delegation = engine.RevokeDelegation(delegationId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["DelegationId"] = delegation.DelegationId,
            ["FromGuardian"] = delegation.FromGuardian,
            ["ToGuardian"] = delegation.ToGuardian,
            ["RoleScope"] = delegation.RoleScope,
            ["Status"] = delegation.Status.ToString()
        });
    }

    private DispatchResult HandleDelegationGet(Dictionary<string, object> payload)
    {
        var delegationId = (string)payload["delegationId"];
        var delegation = _delegationStore.Get(delegationId);

        if (delegation is null)
            return DispatchResult.Fail($"Delegation not found: {delegationId}");

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["DelegationId"] = delegation.DelegationId,
            ["FromGuardian"] = delegation.FromGuardian,
            ["ToGuardian"] = delegation.ToGuardian,
            ["RoleScope"] = delegation.RoleScope,
            ["StartTime"] = delegation.StartTime,
            ["EndTime"] = delegation.EndTime,
            ["Status"] = delegation.Status.ToString()
        });
    }

    private DispatchResult HandleDelegationListByGuardian(Dictionary<string, object> payload)
    {
        var guardianId = (string)payload["guardianId"];
        var delegations = _delegationStore.GetByGuardian(guardianId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["guardianId"] = guardianId,
            ["delegations"] = delegations.Select(d => new Dictionary<string, object>
            {
                ["DelegationId"] = d.DelegationId,
                ["FromGuardian"] = d.FromGuardian,
                ["ToGuardian"] = d.ToGuardian,
                ["RoleScope"] = d.RoleScope,
                ["StartTime"] = d.StartTime,
                ["EndTime"] = d.EndTime,
                ["Status"] = d.Status.ToString()
            }).ToList()
        });
    }
}
