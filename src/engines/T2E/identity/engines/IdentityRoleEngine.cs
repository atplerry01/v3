namespace Whycespace.Engines.T2E.Identity.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("IdentityRoleEngine", EngineTier.T2E, EngineKind.Mutation, "IdentityRoleMutationRequest", typeof(EngineEvent))]
public sealed class IdentityRoleEngine : IEngine
{
    public string Name => "IdentityRoleEngine";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var operation = context.Data.GetValueOrDefault("operation") as string;
        if (string.IsNullOrEmpty(operation))
            return Task.FromResult(EngineResult.Fail("Missing operation"));

        return operation switch
        {
            "assign" => AssignRole(context),
            "revoke" => RevokeRole(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown operation: {operation}"))
        };
    }

    private static Task<EngineResult> AssignRole(EngineContext context)
    {
        var identityId = context.Data.GetValueOrDefault("identityId") as string;
        if (string.IsNullOrEmpty(identityId))
            return Task.FromResult(EngineResult.Fail("Missing identityId"));

        if (!Guid.TryParse(identityId, out var identityGuid) || identityGuid == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid identityId"));

        var roleId = context.Data.GetValueOrDefault("roleId") as string;
        if (string.IsNullOrEmpty(roleId))
            return Task.FromResult(EngineResult.Fail("Missing roleId"));

        var roleName = context.Data.GetValueOrDefault("roleName") as string;
        if (string.IsNullOrEmpty(roleName))
            return Task.FromResult(EngineResult.Fail("Missing roleName"));

        var grantedBy = context.Data.GetValueOrDefault("grantedBy") as string;
        if (string.IsNullOrEmpty(grantedBy))
            return Task.FromResult(EngineResult.Fail("Missing grantedBy"));

        if (!Guid.TryParse(grantedBy, out var grantedByGuid))
            return Task.FromResult(EngineResult.Fail("Invalid grantedBy"));

        var timestamp = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("IdentityRoleAssigned", identityGuid,
                new Dictionary<string, object>
                {
                    ["identityId"] = identityId,
                    ["roleId"] = roleId,
                    ["roleName"] = roleName,
                    ["grantedBy"] = grantedBy,
                    ["grantedAt"] = timestamp.ToString("O"),
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.identity.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["identityId"] = identityId,
                ["roleId"] = roleId,
                ["roleName"] = roleName,
                ["mutationType"] = "Assigned",
                ["executedBy"] = grantedBy,
                ["executedAt"] = timestamp.ToString("O")
            }));
    }

    private static Task<EngineResult> RevokeRole(EngineContext context)
    {
        var identityId = context.Data.GetValueOrDefault("identityId") as string;
        if (string.IsNullOrEmpty(identityId))
            return Task.FromResult(EngineResult.Fail("Missing identityId"));

        if (!Guid.TryParse(identityId, out var identityGuid) || identityGuid == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid identityId"));

        var roleId = context.Data.GetValueOrDefault("roleId") as string;
        if (string.IsNullOrEmpty(roleId))
            return Task.FromResult(EngineResult.Fail("Missing roleId"));

        var revokedBy = context.Data.GetValueOrDefault("revokedBy") as string;
        if (string.IsNullOrEmpty(revokedBy))
            return Task.FromResult(EngineResult.Fail("Missing revokedBy"));

        if (!Guid.TryParse(revokedBy, out var revokedByGuid))
            return Task.FromResult(EngineResult.Fail("Invalid revokedBy"));

        var timestamp = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("IdentityRoleRevoked", identityGuid,
                new Dictionary<string, object>
                {
                    ["identityId"] = identityId,
                    ["roleId"] = roleId,
                    ["revokedBy"] = revokedBy,
                    ["revokedAt"] = timestamp.ToString("O"),
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.identity.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["identityId"] = identityId,
                ["roleId"] = roleId,
                ["roleName"] = string.Empty,
                ["mutationType"] = "Revoked",
                ["executedBy"] = revokedBy,
                ["executedAt"] = timestamp.ToString("O")
            }));
    }
}
