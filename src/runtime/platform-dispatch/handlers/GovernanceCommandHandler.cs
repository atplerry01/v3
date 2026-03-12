using Whycespace.Contracts.Runtime;
using Whycespace.Engines.T0U.Governance;
using Whycespace.System.Upstream.Governance.Stores;
using Whycespace.System.WhyceID.Registry;

namespace Whycespace.Runtime.PlatformDispatch.Handlers;

public sealed class GovernanceCommandHandler
{
    private readonly GuardianRegistryStore _guardianRegistryStore;
    private readonly GovernanceRoleStore _governanceRoleStore;
    private readonly IdentityRegistry _identityRegistry;

    public GovernanceCommandHandler(
        GuardianRegistryStore guardianRegistryStore,
        GovernanceRoleStore governanceRoleStore,
        IdentityRegistry identityRegistry)
    {
        _guardianRegistryStore = guardianRegistryStore;
        _governanceRoleStore = governanceRoleStore;
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
        var engine = new GovernanceRoleEngine(_governanceRoleStore, _guardianRegistryStore);

        var roleId = (string)payload["roleId"];
        var name = (string)payload["name"];
        var description = (string)payload["description"];
        var permissions = (List<string>)payload["permissions"];

        var role = engine.CreateRole(roleId, name, description, permissions);

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
        var engine = new GovernanceRoleEngine(_governanceRoleStore, _guardianRegistryStore);

        var guardianId = (string)payload["guardianId"];
        var roleId = (string)payload["roleId"];

        engine.AssignRole(guardianId, roleId);
        var roles = engine.GetGuardianRoles(guardianId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["guardianId"] = guardianId,
            ["roles"] = roles.Select(r => new Dictionary<string, object>
            {
                ["RoleId"] = r.RoleId,
                ["Name"] = r.Name,
                ["Description"] = r.Description,
                ["Permissions"] = r.Permissions
            }).ToList()
        });
    }

    private DispatchResult HandleRoleRevoke(Dictionary<string, object> payload)
    {
        var engine = new GovernanceRoleEngine(_governanceRoleStore, _guardianRegistryStore);

        var guardianId = (string)payload["guardianId"];
        var roleId = (string)payload["roleId"];

        engine.RevokeRole(guardianId, roleId);
        var roles = engine.GetGuardianRoles(guardianId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["guardianId"] = guardianId,
            ["roles"] = roles.Select(r => new Dictionary<string, object>
            {
                ["RoleId"] = r.RoleId,
                ["Name"] = r.Name,
                ["Description"] = r.Description,
                ["Permissions"] = r.Permissions
            }).ToList()
        });
    }

    private DispatchResult HandleGetGuardianRoles(Dictionary<string, object> payload)
    {
        var engine = new GovernanceRoleEngine(_governanceRoleStore, _guardianRegistryStore);

        var guardianId = (string)payload["guardianId"];
        var roles = engine.GetGuardianRoles(guardianId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["guardianId"] = guardianId,
            ["roles"] = roles.Select(r => new Dictionary<string, object>
            {
                ["RoleId"] = r.RoleId,
                ["Name"] = r.Name,
                ["Description"] = r.Description,
                ["Permissions"] = r.Permissions
            }).ToList()
        });
    }
}
