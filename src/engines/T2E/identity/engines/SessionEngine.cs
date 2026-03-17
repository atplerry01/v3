namespace Whycespace.Engines.T2E.Identity.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("Session", EngineTier.T2E, EngineKind.Mutation, "CreateSessionCommand", typeof(EngineEvent))]
public sealed class SessionEngine : IEngine
{
    private static readonly HashSet<string> ValidAuthenticationMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password", "Token", "OAuth", "APIKey"
    };

    private const double MinimumDeviceTrustScore = 0.5;
    private const int DefaultSessionDurationHours = 8;

    public string Name => "Session";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var operation = context.Data.GetValueOrDefault("operation") as string ?? "create";

        return operation switch
        {
            "create" => CreateSession(context),
            "validate" => ValidateSession(context),
            "revoke" => RevokeSession(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown operation: {operation}"))
        };
    }

    private Task<EngineResult> CreateSession(EngineContext context)
    {
        // 1. Validate command input
        var identityId = context.Data.GetValueOrDefault("identityId") as string;
        if (string.IsNullOrEmpty(identityId) || !Guid.TryParse(identityId, out var identityGuid) || identityGuid == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Missing or invalid identityId"));

        var deviceId = context.Data.GetValueOrDefault("deviceId") as string;
        if (string.IsNullOrEmpty(deviceId))
            return Task.FromResult(EngineResult.Fail("Missing deviceId"));

        var authenticationMethod = context.Data.GetValueOrDefault("authenticationMethod") as string;
        if (string.IsNullOrEmpty(authenticationMethod) || !ValidAuthenticationMethods.Contains(authenticationMethod))
            return Task.FromResult(EngineResult.Fail($"Invalid or missing authenticationMethod. Valid: {string.Join(", ", ValidAuthenticationMethods)}"));

        // 2. Verify authentication result
        var authenticated = context.Data.GetValueOrDefault("authenticated");
        if (authenticated is false or "false")
            return Task.FromResult(EngineResult.Fail("Identity not authenticated"));

        // 3. Verify identity status is Active
        var identityStatus = context.Data.GetValueOrDefault("identityStatus") as string;
        if (!string.Equals(identityStatus, "Active", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(EngineResult.Fail("Identity is not active"));

        // 4. Validate device trust score
        var deviceTrustScore = ResolveDouble(context.Data.GetValueOrDefault("deviceTrustScore"), 0.0);
        if (deviceTrustScore < MinimumDeviceTrustScore)
            return Task.FromResult(EngineResult.Fail("Device trust score below threshold"));

        // 5. Generate session identifier and token
        var sessionId = Guid.NewGuid();
        var sessionToken = GenerateSessionToken(sessionId, identityGuid);

        // 6. Calculate expiration
        var issuedAt = DateTime.UtcNow;
        var expiresAt = issuedAt.AddHours(DefaultSessionDurationHours);

        // 7. Emit SessionCreatedEvent
        var ipAddress = context.Data.GetValueOrDefault("ipAddress") as string ?? "";
        var geoLocation = context.Data.GetValueOrDefault("geoLocation") as string ?? "";

        var events = new[]
        {
            EngineEvent.Create("SessionCreated", identityGuid,
                new Dictionary<string, object>
                {
                    ["sessionId"] = sessionId.ToString(),
                    ["identityId"] = identityId,
                    ["deviceId"] = deviceId,
                    ["authenticationMethod"] = authenticationMethod,
                    ["issuedAt"] = issuedAt.ToString("O"),
                    ["expiresAt"] = expiresAt.ToString("O"),
                    ["ipAddress"] = ipAddress,
                    ["geoLocation"] = geoLocation,
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.identity.events"
                })
        };

        // 8. Return SessionResult
        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["sessionId"] = sessionId.ToString(),
                ["identityId"] = identityId,
                ["sessionToken"] = sessionToken,
                ["issuedAt"] = issuedAt.ToString("O"),
                ["expiresAt"] = expiresAt.ToString("O"),
                ["active"] = true
            }));
    }

    private static Task<EngineResult> ValidateSession(EngineContext context)
    {
        // 1. Validate command input
        var sessionId = context.Data.GetValueOrDefault("sessionId") as string;
        if (string.IsNullOrEmpty(sessionId) || !Guid.TryParse(sessionId, out var sessionGuid) || sessionGuid == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Missing or invalid sessionId"));

        var identityId = context.Data.GetValueOrDefault("identityId") as string;
        if (string.IsNullOrEmpty(identityId) || !Guid.TryParse(identityId, out var identityGuid))
            return Task.FromResult(EngineResult.Fail("Missing or invalid identityId"));

        // 2. Check if session is active
        var sessionActive = context.Data.GetValueOrDefault("sessionActive");
        if (sessionActive is false or "false")
            return Task.FromResult(SessionValidationFailed(sessionGuid, identityGuid, "Session is not active"));

        // 3. Check if session has expired
        var expiresAtStr = context.Data.GetValueOrDefault("expiresAt") as string;
        if (!string.IsNullOrEmpty(expiresAtStr) && DateTime.TryParse(expiresAtStr, out var expiresAt))
        {
            if (DateTime.UtcNow >= expiresAt)
                return Task.FromResult(SessionValidationFailed(sessionGuid, identityGuid, "Session has expired"));
        }

        // 4. Check if identity has been revoked
        var identityRevoked = context.Data.GetValueOrDefault("identityRevoked");
        if (identityRevoked is true or "true")
            return Task.FromResult(SessionValidationFailed(sessionGuid, identityGuid, "Identity has been revoked"));

        // 5. Check if device risk detected
        var deviceRiskDetected = context.Data.GetValueOrDefault("deviceRiskDetected");
        if (deviceRiskDetected is true or "true")
            return Task.FromResult(SessionValidationFailed(sessionGuid, identityGuid, "Device risk detected"));

        // 6. Session is valid
        var validatedAt = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("SessionValidated", identityGuid,
                new Dictionary<string, object>
                {
                    ["sessionId"] = sessionId,
                    ["identityId"] = identityId,
                    ["valid"] = true,
                    ["validatedAt"] = validatedAt.ToString("O"),
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.identity.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["valid"] = true,
                ["identityId"] = identityId,
                ["reason"] = "",
                ["validatedAt"] = validatedAt.ToString("O")
            }));
    }

    private static Task<EngineResult> RevokeSession(EngineContext context)
    {
        // 1. Validate command input
        var sessionId = context.Data.GetValueOrDefault("sessionId") as string;
        if (string.IsNullOrEmpty(sessionId) || !Guid.TryParse(sessionId, out var sessionGuid) || sessionGuid == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Missing or invalid sessionId"));

        var identityId = context.Data.GetValueOrDefault("identityId") as string;
        if (string.IsNullOrEmpty(identityId) || !Guid.TryParse(identityId, out var identityGuid))
            return Task.FromResult(EngineResult.Fail("Missing or invalid identityId"));

        // 2. Revoke session
        var revokedAt = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("SessionRevoked", identityGuid,
                new Dictionary<string, object>
                {
                    ["sessionId"] = sessionId,
                    ["identityId"] = identityId,
                    ["revokedAt"] = revokedAt.ToString("O"),
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.identity.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["identityId"] = identityId,
                ["revoked"] = true,
                ["revokedAt"] = revokedAt.ToString("O")
            }));
    }

    private static EngineResult SessionValidationFailed(Guid sessionId, Guid identityId, string reason)
    {
        var validatedAt = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("SessionValidated", identityId,
                new Dictionary<string, object>
                {
                    ["sessionId"] = sessionId.ToString(),
                    ["identityId"] = identityId.ToString(),
                    ["valid"] = false,
                    ["reason"] = reason,
                    ["validatedAt"] = validatedAt.ToString("O"),
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.identity.events"
                })
        };

        return new EngineResult(false, events,
            new Dictionary<string, object>
            {
                ["sessionId"] = sessionId.ToString(),
                ["valid"] = false,
                ["identityId"] = identityId.ToString(),
                ["reason"] = reason,
                ["validatedAt"] = validatedAt.ToString("O")
            });
    }

    private static string GenerateSessionToken(Guid sessionId, Guid identityId)
    {
        var tokenData = $"{sessionId:N}{identityId:N}{DateTime.UtcNow.Ticks}";
        var bytes = global::System.Text.Encoding.UTF8.GetBytes(tokenData);
        return Convert.ToBase64String(bytes);
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
