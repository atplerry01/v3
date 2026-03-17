namespace Whycespace.Engines.T2E.System.Identity;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("Authentication", EngineTier.T2E, EngineKind.Validation, "AuthenticateIdentityCommand", typeof(EngineEvent))]
public sealed class AuthenticationEngine : IEngine
{
    private static readonly HashSet<string> ValidAuthenticationMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password", "Token", "OAuth", "APIKey"
    };

    private const double MinimumDeviceTrustScore = 0.5;

    public string Name => "Authentication";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        // 1. Validate command input
        var identityId = context.Data.GetValueOrDefault("identityId") as string;
        if (string.IsNullOrEmpty(identityId) || !Guid.TryParse(identityId, out var identityGuid) || identityGuid == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Missing or invalid identityId"));

        var credentialType = context.Data.GetValueOrDefault("credentialType") as string;
        if (string.IsNullOrEmpty(credentialType))
            return Task.FromResult(EngineResult.Fail("Missing credentialType"));

        var credentialValue = context.Data.GetValueOrDefault("credentialValue") as string;
        if (string.IsNullOrEmpty(credentialValue))
            return Task.FromResult(EngineResult.Fail("Missing credentialValue"));

        var deviceId = context.Data.GetValueOrDefault("deviceId") as string;
        if (string.IsNullOrEmpty(deviceId))
            return Task.FromResult(EngineResult.Fail("Missing deviceId"));

        var authenticationMethod = context.Data.GetValueOrDefault("authenticationMethod") as string;
        if (string.IsNullOrEmpty(authenticationMethod) || !ValidAuthenticationMethods.Contains(authenticationMethod))
            return Task.FromResult(EngineResult.Fail($"Invalid or missing authenticationMethod. Valid: {string.Join(", ", ValidAuthenticationMethods)}"));

        // 2. Verify identity exists
        var identityExists = context.Data.GetValueOrDefault("identityExists");
        if (identityExists is false or "false")
            return Task.FromResult(FailAuthentication(identityGuid, authenticationMethod, 0.0, "Identity does not exist"));

        // 3. Verify identity status is Active
        var identityStatus = context.Data.GetValueOrDefault("identityStatus") as string;
        if (!string.Equals(identityStatus, "Active", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(FailAuthentication(identityGuid, authenticationMethod, 0.0, "Identity is not active"));

        // 4. Validate credential
        var credentialValid = context.Data.GetValueOrDefault("credentialValid");
        if (credentialValid is false or "false")
            return Task.FromResult(FailAuthentication(identityGuid, authenticationMethod, 0.0, "Invalid credentials"));

        // 5. Evaluate device trust score
        var deviceTrustScore = ResolveDouble(context.Data.GetValueOrDefault("deviceTrustScore"), 0.0);
        if (deviceTrustScore < MinimumDeviceTrustScore)
            return Task.FromResult(FailAuthentication(identityGuid, authenticationMethod, deviceTrustScore, "Untrusted device"));

        // 6. Authentication successful - emit event
        var authenticatedAt = DateTime.UtcNow;
        var deviceFingerprint = context.Data.GetValueOrDefault("deviceFingerprint") as string ?? "";

        var events = new[]
        {
            EngineEvent.Create("IdentityAuthenticated", identityGuid,
                new Dictionary<string, object>
                {
                    ["identityId"] = identityId,
                    ["authenticationMethod"] = authenticationMethod,
                    ["deviceId"] = deviceId,
                    ["deviceFingerprint"] = deviceFingerprint,
                    ["deviceTrustScore"] = deviceTrustScore,
                    ["authenticatedAt"] = authenticatedAt.ToString("O"),
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.identity.events"
                })
        };

        // 7. Return AuthenticationResult
        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["identityId"] = identityId,
                ["authenticated"] = true,
                ["authenticationMethod"] = authenticationMethod,
                ["deviceTrustScore"] = deviceTrustScore,
                ["failureReason"] = "",
                ["authenticatedAt"] = authenticatedAt.ToString("O")
            }));
    }

    private static EngineResult FailAuthentication(Guid identityId, string authenticationMethod, double deviceTrustScore, string reason)
    {
        var output = new Dictionary<string, object>
        {
            ["identityId"] = identityId.ToString(),
            ["authenticated"] = false,
            ["authenticationMethod"] = authenticationMethod,
            ["deviceTrustScore"] = deviceTrustScore,
            ["failureReason"] = reason,
            ["authenticatedAt"] = ""
        };

        return new EngineResult(false, Array.Empty<EngineEvent>(), output);
    }

    private static double ResolveDouble(object? value, double fallback)
    {
        return value switch
        {
            double d => d,
            float f => f,
            decimal m => (double)m,
            int i => i,
            long l => l,
            string s when double.TryParse(s, out var parsed) => parsed,
            _ => fallback
        };
    }
}
