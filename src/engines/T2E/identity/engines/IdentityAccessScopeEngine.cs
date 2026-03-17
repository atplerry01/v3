namespace Whycespace.Engines.T2E.Identity.Engines;

using global::System.Text.RegularExpressions;
using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("IdentityAccessScopeEngine", EngineTier.T2E, EngineKind.Mutation, "IdentityScopeMutationRequest", typeof(EngineEvent))]
public sealed class IdentityAccessScopeEngine : IEngine
{
    private static readonly Regex ScopePattern = new(
        @"^(cluster|spv|domain|system):[a-z][a-z0-9\-]*$",
        RegexOptions.Compiled);

    public string Name => "IdentityAccessScopeEngine";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var operation = context.Data.GetValueOrDefault("operation") as string;
        if (string.IsNullOrEmpty(operation))
            return Task.FromResult(EngineResult.Fail("Missing operation"));

        return operation switch
        {
            "grant" => GrantScope(context),
            "revoke" => RevokeScope(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown operation: {operation}"))
        };
    }

    private static Task<EngineResult> GrantScope(EngineContext context)
    {
        var identityId = context.Data.GetValueOrDefault("identityId") as string;
        if (string.IsNullOrEmpty(identityId))
            return Task.FromResult(EngineResult.Fail("Missing identityId"));

        if (!Guid.TryParse(identityId, out var identityGuid) || identityGuid == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid identityId"));

        var scopeKey = context.Data.GetValueOrDefault("scopeKey") as string;
        if (string.IsNullOrEmpty(scopeKey))
            return Task.FromResult(EngineResult.Fail("Missing scopeKey"));

        if (!ScopePattern.IsMatch(scopeKey))
            return Task.FromResult(EngineResult.Fail($"Invalid scope format: {scopeKey}. Expected pattern: cluster:name, spv:name, domain:name, or system:name"));

        var grantedBy = context.Data.GetValueOrDefault("grantedBy") as string;
        if (string.IsNullOrEmpty(grantedBy))
            return Task.FromResult(EngineResult.Fail("Missing grantedBy"));

        if (!Guid.TryParse(grantedBy, out var grantedByGuid))
            return Task.FromResult(EngineResult.Fail("Invalid grantedBy"));

        var timestamp = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("IdentityScopeGranted", identityGuid,
                new Dictionary<string, object>
                {
                    ["identityId"] = identityId,
                    ["scopeKey"] = scopeKey,
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
                ["scopeKey"] = scopeKey,
                ["mutationType"] = "Granted",
                ["executedBy"] = grantedBy,
                ["executedAt"] = timestamp.ToString("O")
            }));
    }

    private static Task<EngineResult> RevokeScope(EngineContext context)
    {
        var identityId = context.Data.GetValueOrDefault("identityId") as string;
        if (string.IsNullOrEmpty(identityId))
            return Task.FromResult(EngineResult.Fail("Missing identityId"));

        if (!Guid.TryParse(identityId, out var identityGuid) || identityGuid == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid identityId"));

        var scopeKey = context.Data.GetValueOrDefault("scopeKey") as string;
        if (string.IsNullOrEmpty(scopeKey))
            return Task.FromResult(EngineResult.Fail("Missing scopeKey"));

        if (!ScopePattern.IsMatch(scopeKey))
            return Task.FromResult(EngineResult.Fail($"Invalid scope format: {scopeKey}. Expected pattern: cluster:name, spv:name, domain:name, or system:name"));

        var revokedBy = context.Data.GetValueOrDefault("revokedBy") as string;
        if (string.IsNullOrEmpty(revokedBy))
            return Task.FromResult(EngineResult.Fail("Missing revokedBy"));

        if (!Guid.TryParse(revokedBy, out var revokedByGuid))
            return Task.FromResult(EngineResult.Fail("Invalid revokedBy"));

        var timestamp = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("IdentityScopeRevoked", identityGuid,
                new Dictionary<string, object>
                {
                    ["identityId"] = identityId,
                    ["scopeKey"] = scopeKey,
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
                ["scopeKey"] = scopeKey,
                ["mutationType"] = "Revoked",
                ["executedBy"] = revokedBy,
                ["executedAt"] = timestamp.ToString("O")
            }));
    }
}
