namespace Whycespace.Engines.T2E.Core.Identity;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("Consent", EngineTier.T2E, EngineKind.Mutation, "GrantConsentCommand", typeof(EngineEvent))]
public sealed class ConsentEngine : IEngine
{
    private static readonly HashSet<string> ValidConsentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "DataProcessing", "Marketing", "Analytics", "ThirdPartySharing", "Profiling", "SystemOperations"
    };

    public string Name => "Consent";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var operation = context.Data.GetValueOrDefault("operation") as string ?? "grant";

        return operation switch
        {
            "grant" => GrantConsent(context),
            "withdraw" => WithdrawConsent(context),
            "validate" => ValidateConsent(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown operation: {operation}"))
        };
    }

    private static Task<EngineResult> GrantConsent(EngineContext context)
    {
        // 1. Validate command input
        var identityId = context.Data.GetValueOrDefault("identityId") as string;
        if (string.IsNullOrEmpty(identityId) || !Guid.TryParse(identityId, out var identityGuid) || identityGuid == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Missing or invalid identityId"));

        var consentType = context.Data.GetValueOrDefault("consentType") as string;
        if (string.IsNullOrEmpty(consentType))
            return Task.FromResult(EngineResult.Fail("Missing consentType"));

        if (!ValidConsentTypes.Contains(consentType))
            return Task.FromResult(EngineResult.Fail($"Invalid consentType. Valid: {string.Join(", ", ValidConsentTypes)}"));

        var consentScope = context.Data.GetValueOrDefault("consentScope") as string;
        if (string.IsNullOrEmpty(consentScope))
            return Task.FromResult(EngineResult.Fail("Missing consentScope"));

        var grantedBy = context.Data.GetValueOrDefault("grantedBy") as string;
        if (string.IsNullOrEmpty(grantedBy))
            return Task.FromResult(EngineResult.Fail("Missing grantedBy"));

        // 2. Evaluate identity status
        var identityExists = context.Data.GetValueOrDefault("identityExists");
        if (identityExists is false or "false")
            return Task.FromResult(EngineResult.Fail("Identity does not exist"));

        var identityStatus = context.Data.GetValueOrDefault("identityStatus") as string;
        if (!string.Equals(identityStatus, "Active", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(EngineResult.Fail("Identity is not active"));

        // 3. Check policy restriction
        var policyDenied = context.Data.GetValueOrDefault("policyDenied");
        if (policyDenied is true or "true")
            return Task.FromResult(EngineResult.Fail("Consent denied by policy"));

        // 4. Grant consent and emit event
        var grantedAt = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("ConsentGranted", identityGuid,
                new Dictionary<string, object>
                {
                    ["identityId"] = identityId,
                    ["consentType"] = consentType,
                    ["scope"] = consentScope,
                    ["grantedBy"] = grantedBy,
                    ["grantedAt"] = grantedAt.ToString("O"),
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.identity.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["identityId"] = identityId,
                ["consentType"] = consentType,
                ["granted"] = true,
                ["scope"] = consentScope,
                ["grantedAt"] = grantedAt.ToString("O")
            }));
    }

    private static Task<EngineResult> WithdrawConsent(EngineContext context)
    {
        // 1. Validate command input
        var identityId = context.Data.GetValueOrDefault("identityId") as string;
        if (string.IsNullOrEmpty(identityId) || !Guid.TryParse(identityId, out var identityGuid) || identityGuid == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Missing or invalid identityId"));

        var consentType = context.Data.GetValueOrDefault("consentType") as string;
        if (string.IsNullOrEmpty(consentType))
            return Task.FromResult(EngineResult.Fail("Missing consentType"));

        var reason = context.Data.GetValueOrDefault("reason") as string;
        if (string.IsNullOrEmpty(reason))
            return Task.FromResult(EngineResult.Fail("Missing reason"));

        // 2. Verify consent was previously granted
        var consentGranted = context.Data.GetValueOrDefault("consentGranted");
        if (consentGranted is false or "false")
            return Task.FromResult(EngineResult.Fail("Consent was not previously granted"));

        // 3. Withdraw consent and emit event
        var withdrawnAt = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("ConsentWithdrawn", identityGuid,
                new Dictionary<string, object>
                {
                    ["identityId"] = identityId,
                    ["consentType"] = consentType,
                    ["reason"] = reason,
                    ["withdrawnAt"] = withdrawnAt.ToString("O"),
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.identity.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["identityId"] = identityId,
                ["consentType"] = consentType,
                ["granted"] = false,
                ["scope"] = "",
                ["grantedAt"] = ""
            }));
    }

    private static Task<EngineResult> ValidateConsent(EngineContext context)
    {
        // 1. Validate command input
        var identityId = context.Data.GetValueOrDefault("identityId") as string;
        if (string.IsNullOrEmpty(identityId) || !Guid.TryParse(identityId, out var identityGuid) || identityGuid == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Missing or invalid identityId"));

        var consentType = context.Data.GetValueOrDefault("consentType") as string;
        if (string.IsNullOrEmpty(consentType))
            return Task.FromResult(EngineResult.Fail("Missing consentType"));

        var requiredScope = context.Data.GetValueOrDefault("requiredScope") as string;
        if (string.IsNullOrEmpty(requiredScope))
            return Task.FromResult(EngineResult.Fail("Missing requiredScope"));

        // 2. Evaluate identity status
        var identityExists = context.Data.GetValueOrDefault("identityExists");
        if (identityExists is false or "false")
            return Task.FromResult(ConsentValidationFailed(identityGuid, consentType, false, "Identity does not exist"));

        // 3. Evaluate consent state
        var consentGranted = context.Data.GetValueOrDefault("consentGranted");
        if (consentGranted is not true and not "true")
            return Task.FromResult(ConsentValidationFailed(identityGuid, consentType, false, "Consent not granted"));

        // 4. Check if consent has been withdrawn
        var consentWithdrawn = context.Data.GetValueOrDefault("consentWithdrawn");
        if (consentWithdrawn is true or "true")
            return Task.FromResult(ConsentValidationFailed(identityGuid, consentType, false, "Consent withdrawn"));

        // 5. Evaluate consent scope
        var grantedScope = context.Data.GetValueOrDefault("consentScope") as string ?? "";
        var scopeValid = EvaluateScope(grantedScope, requiredScope);
        if (!scopeValid)
            return Task.FromResult(ConsentValidationFailed(identityGuid, consentType, false, "Scope mismatch"));

        // 6. Check policy constraints
        var policyDenied = context.Data.GetValueOrDefault("policyDenied");
        if (policyDenied is true or "true")
            return Task.FromResult(ConsentValidationFailed(identityGuid, consentType, true, "Policy restriction"));

        // 7. Consent is valid
        var validatedAt = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("ConsentValidated", identityGuid,
                new Dictionary<string, object>
                {
                    ["identityId"] = identityId,
                    ["consentType"] = consentType,
                    ["valid"] = true,
                    ["validatedAt"] = validatedAt.ToString("O"),
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.identity.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["identityId"] = identityId,
                ["consentType"] = consentType,
                ["valid"] = true,
                ["scopeValidated"] = true,
                ["reason"] = "Consent valid",
                ["validatedAt"] = validatedAt.ToString("O")
            }));
    }

    private static EngineResult ConsentValidationFailed(Guid identityId, string consentType, bool scopeValidated, string reason)
    {
        var validatedAt = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("ConsentValidated", identityId,
                new Dictionary<string, object>
                {
                    ["identityId"] = identityId.ToString(),
                    ["consentType"] = consentType,
                    ["valid"] = false,
                    ["validatedAt"] = validatedAt.ToString("O"),
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.identity.events"
                })
        };

        return new EngineResult(false, events,
            new Dictionary<string, object>
            {
                ["identityId"] = identityId.ToString(),
                ["consentType"] = consentType,
                ["valid"] = false,
                ["scopeValidated"] = scopeValidated,
                ["reason"] = reason,
                ["validatedAt"] = validatedAt.ToString("O")
            });
    }

    private static bool EvaluateScope(string grantedScope, string requiredScope)
    {
        if (string.IsNullOrEmpty(grantedScope) || string.IsNullOrEmpty(requiredScope))
            return false;

        var grantedScopes = grantedScope.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var requiredScopes = requiredScope.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return Array.TrueForAll(requiredScopes, r =>
            Array.Exists(grantedScopes, g => string.Equals(g, r, StringComparison.OrdinalIgnoreCase)));
    }
}
