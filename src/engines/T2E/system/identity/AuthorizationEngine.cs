namespace Whycespace.Engines.T2E.System.Identity;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("AuthorizationEngine", EngineTier.T2E, EngineKind.Decision, "AuthorizeActionCommand", typeof(EngineEvent))]
public sealed class AuthorizationEngine : IEngine
{
    public string Name => "AuthorizationEngine";

    private const double MinTrustScore = 0.5;
    private const double MinDeviceTrustScore = 0.3;

    private static readonly string[] ValidScopes =
        { "global", "cluster", "spv", "vault", "resource", "system" };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var identityId = context.Data.GetValueOrDefault("identityId") as string;
        if (string.IsNullOrEmpty(identityId))
            return Task.FromResult(EngineResult.Fail("Missing identityId"));

        if (!Guid.TryParse(identityId, out var identityGuid) || identityGuid == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid identityId"));

        var resourceType = context.Data.GetValueOrDefault("resourceType") as string;
        if (string.IsNullOrEmpty(resourceType))
            return Task.FromResult(EngineResult.Fail("Missing resourceType"));

        var resourceId = context.Data.GetValueOrDefault("resourceId") as string;
        if (string.IsNullOrEmpty(resourceId) || !Guid.TryParse(resourceId, out _))
            return Task.FromResult(EngineResult.Fail("Missing or invalid resourceId"));

        var action = context.Data.GetValueOrDefault("action") as string;
        if (string.IsNullOrEmpty(action))
            return Task.FromResult(EngineResult.Fail("Missing action"));

        var requiredPermission = context.Data.GetValueOrDefault("requiredPermission") as string;
        if (string.IsNullOrEmpty(requiredPermission))
            return Task.FromResult(EngineResult.Fail("Missing requiredPermission"));

        var accessScope = context.Data.GetValueOrDefault("accessScope") as string;
        if (string.IsNullOrEmpty(accessScope))
            return Task.FromResult(EngineResult.Fail("Missing accessScope"));

        var authenticated = context.Data.GetValueOrDefault("authenticated");
        if (authenticated is not true and not "true")
            return Deny(identityId, identityGuid, resourceType, action, requiredPermission, accessScope, "Identity is not authenticated");

        var trustScore = ResolveDouble(context.Data.GetValueOrDefault("trustScore"));
        if (trustScore is null)
            return Task.FromResult(EngineResult.Fail("Missing or invalid trustScore"));

        var deviceTrustScore = ResolveDouble(context.Data.GetValueOrDefault("deviceTrustScore"));
        if (deviceTrustScore is null)
            return Task.FromResult(EngineResult.Fail("Missing or invalid deviceTrustScore"));

        // Evaluate trust score thresholds
        if (trustScore.Value < MinTrustScore)
            return Deny(identityId, identityGuid, resourceType, action, requiredPermission, accessScope, $"Trust score {trustScore.Value} below minimum threshold {MinTrustScore}");

        if (deviceTrustScore.Value < MinDeviceTrustScore)
            return Deny(identityId, identityGuid, resourceType, action, requiredPermission, accessScope, $"Device trust score {deviceTrustScore.Value} below minimum threshold {MinDeviceTrustScore}");

        // Validate access scope
        if (!Array.Exists(ValidScopes, s => s == accessScope))
            return Deny(identityId, identityGuid, resourceType, action, requiredPermission, accessScope, $"Invalid access scope: {accessScope}");

        // Validate role permissions
        var permissions = context.Data.GetValueOrDefault("permissions") as string;
        if (string.IsNullOrEmpty(permissions))
            return Deny(identityId, identityGuid, resourceType, action, requiredPermission, accessScope, "No permissions assigned");

        var permissionList = permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!Array.Exists(permissionList, p => p == requiredPermission))
            return Deny(identityId, identityGuid, resourceType, action, requiredPermission, accessScope, $"Missing required permission: {requiredPermission}");

        // Check policy denial
        var policyDenied = context.Data.GetValueOrDefault("policyDenied");
        if (policyDenied is true or "true")
            return Deny(identityId, identityGuid, resourceType, action, requiredPermission, accessScope, "Access denied by policy");

        // Authorization granted
        return Authorize(identityId, identityGuid, resourceType, action, requiredPermission, accessScope);
    }

    private static Task<EngineResult> Authorize(
        string identityId, Guid identityGuid, string resourceType, string action,
        string requiredPermission, string accessScope)
    {
        var timestamp = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("AuthorizationEvaluated", identityGuid,
                new Dictionary<string, object>
                {
                    ["identityId"] = identityId,
                    ["resourceType"] = resourceType,
                    ["action"] = action,
                    ["authorized"] = true,
                    ["evaluatedAt"] = timestamp.ToString("O"),
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.identity.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["identityId"] = identityId,
                ["authorized"] = true,
                ["permissionGranted"] = requiredPermission,
                ["scopeValidated"] = true,
                ["reason"] = "Authorization granted",
                ["evaluatedAt"] = timestamp.ToString("O")
            }));
    }

    private static Task<EngineResult> Deny(
        string identityId, Guid identityGuid, string resourceType, string action,
        string requiredPermission, string accessScope, string reason)
    {
        var timestamp = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("AuthorizationEvaluated", identityGuid,
                new Dictionary<string, object>
                {
                    ["identityId"] = identityId,
                    ["resourceType"] = resourceType,
                    ["action"] = action,
                    ["authorized"] = false,
                    ["evaluatedAt"] = timestamp.ToString("O"),
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.identity.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["identityId"] = identityId,
                ["authorized"] = false,
                ["permissionGranted"] = requiredPermission,
                ["scopeValidated"] = false,
                ["reason"] = reason,
                ["evaluatedAt"] = timestamp.ToString("O")
            }));
    }

    private static double? ResolveDouble(object? value)
    {
        return value switch
        {
            double d => d,
            decimal d => (double)d,
            int i => i,
            long l => l,
            float f => f,
            string s when double.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }
}
