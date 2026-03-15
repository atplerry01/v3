namespace Whycespace.Engines.T2E.System.Identity;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("IdentityPermissionEngine", EngineTier.T2E, EngineKind.Mutation, "IdentityPermissionMutationRequest", typeof(EngineEvent))]
public sealed class IdentityPermissionEngine : IEngine
{
    public string Name => "IdentityPermissionEngine";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var operation = context.Data.GetValueOrDefault("operation") as string;
        if (string.IsNullOrEmpty(operation))
            return Task.FromResult(EngineResult.Fail("Missing operation"));

        return operation switch
        {
            "grant" => GrantPermission(context),
            "revoke" => RevokePermission(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown operation: {operation}"))
        };
    }

    private static Task<EngineResult> GrantPermission(EngineContext context)
    {
        var identityId = context.Data.GetValueOrDefault("identityId") as string;
        if (string.IsNullOrEmpty(identityId))
            return Task.FromResult(EngineResult.Fail("Missing identityId"));

        if (!Guid.TryParse(identityId, out var identityGuid) || identityGuid == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid identityId"));

        var permissionKey = context.Data.GetValueOrDefault("permissionKey") as string;
        if (string.IsNullOrEmpty(permissionKey))
            return Task.FromResult(EngineResult.Fail("Missing permissionKey"));

        var grantedBy = context.Data.GetValueOrDefault("grantedBy") as string;
        if (string.IsNullOrEmpty(grantedBy))
            return Task.FromResult(EngineResult.Fail("Missing grantedBy"));

        if (!Guid.TryParse(grantedBy, out var grantedByGuid))
            return Task.FromResult(EngineResult.Fail("Invalid grantedBy"));

        var timestamp = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("IdentityPermissionGranted", identityGuid,
                new Dictionary<string, object>
                {
                    ["identityId"] = identityId,
                    ["permissionKey"] = permissionKey,
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
                ["permissionKey"] = permissionKey,
                ["mutationType"] = "Granted",
                ["executedBy"] = grantedBy,
                ["executedAt"] = timestamp.ToString("O")
            }));
    }

    private static Task<EngineResult> RevokePermission(EngineContext context)
    {
        var identityId = context.Data.GetValueOrDefault("identityId") as string;
        if (string.IsNullOrEmpty(identityId))
            return Task.FromResult(EngineResult.Fail("Missing identityId"));

        if (!Guid.TryParse(identityId, out var identityGuid) || identityGuid == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid identityId"));

        var permissionKey = context.Data.GetValueOrDefault("permissionKey") as string;
        if (string.IsNullOrEmpty(permissionKey))
            return Task.FromResult(EngineResult.Fail("Missing permissionKey"));

        var revokedBy = context.Data.GetValueOrDefault("revokedBy") as string;
        if (string.IsNullOrEmpty(revokedBy))
            return Task.FromResult(EngineResult.Fail("Missing revokedBy"));

        if (!Guid.TryParse(revokedBy, out var revokedByGuid))
            return Task.FromResult(EngineResult.Fail("Invalid revokedBy"));

        var timestamp = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("IdentityPermissionRevoked", identityGuid,
                new Dictionary<string, object>
                {
                    ["identityId"] = identityId,
                    ["permissionKey"] = permissionKey,
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
                ["permissionKey"] = permissionKey,
                ["mutationType"] = "Revoked",
                ["executedBy"] = revokedBy,
                ["executedAt"] = timestamp.ToString("O")
            }));
    }
}
