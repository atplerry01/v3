using Whycespace.Engines.T3I.Atlas.Identity.Models;
namespace Whycespace.Engines.T3I.Atlas.Identity.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Engines.T3I.Shared;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("DeviceTrust", EngineTier.T3I, EngineKind.Decision, "EvaluateDeviceTrustCommand", typeof(EngineEvent))]
public sealed class DeviceTrustEngine : IEngine, IIntelligenceEngine<EngineContext, EngineResult>
{
    public string Name => "DeviceTrust";
    public string EngineName => "DeviceTrust";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var intelligenceContext = IntelligenceContext<EngineContext>.Create(context.InvocationId, context);
        var result = Execute(intelligenceContext);
        return Task.FromResult(result.Success ? result.Output! : EngineResult.Fail(result.Error!));
    }

    public IntelligenceResult<EngineResult> Execute(IntelligenceContext<EngineContext> context)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var engineContext = context.Input;

        var identityId = engineContext.Data.GetValueOrDefault("identityId") as string;
        if (string.IsNullOrEmpty(identityId) || !Guid.TryParse(identityId, out var identityGuid))
            return IntelligenceResult<EngineResult>.Fail("Missing or invalid identityId",
                IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));

        var deviceId = engineContext.Data.GetValueOrDefault("deviceId") as string;
        if (string.IsNullOrEmpty(deviceId))
            return IntelligenceResult<EngineResult>.Fail("Missing deviceId",
                IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));

        var deviceFingerprint = engineContext.Data.GetValueOrDefault("deviceFingerprint") as string ?? "";
        var deviceType = engineContext.Data.GetValueOrDefault("deviceType") as string ?? "";
        var operatingSystem = engineContext.Data.GetValueOrDefault("operatingSystem") as string ?? "";
        var ipAddress = engineContext.Data.GetValueOrDefault("ipAddress") as string ?? "";
        var geoLocation = engineContext.Data.GetValueOrDefault("geoLocation") as string ?? "";
        var previousDeviceUsageCount = ResolveInt(engineContext.Data.GetValueOrDefault("previousDeviceUsageCount")) ?? 0;
        var deviceAgeDays = ResolveInt(engineContext.Data.GetValueOrDefault("deviceAgeDays")) ?? 0;

        var scoreFactors = new Dictionary<string, double>();
        var riskIndicators = new List<string>();

        // Known device (previous usage > 0): +20
        if (previousDeviceUsageCount > 0)
        {
            scoreFactors["knownDevice"] = 20.0;
        }
        else
        {
            riskIndicators.Add("Unknown device — no prior usage history");
        }

        // Stable fingerprint (non-empty): +20
        if (!string.IsNullOrEmpty(deviceFingerprint))
        {
            scoreFactors["stableFingerprint"] = 20.0;
        }
        else
        {
            riskIndicators.Add("Missing device fingerprint");
        }

        // Consistent geolocation (non-empty): +10
        if (!string.IsNullOrEmpty(geoLocation))
        {
            scoreFactors["consistentGeoLocation"] = 10.0;
        }
        else
        {
            riskIndicators.Add("Missing geolocation data");
        }

        // Device usage history: up to +20 (scaled, max at 50 usages)
        var usageComponent = Math.Min(previousDeviceUsageCount, 50) / 50.0 * 20.0;
        scoreFactors["deviceUsageHistory"] = Math.Round(usageComponent, 2);

        // Low-risk IP reputation (non-empty IP): +15
        if (!string.IsNullOrEmpty(ipAddress))
        {
            scoreFactors["ipReputation"] = 15.0;
        }
        else
        {
            riskIndicators.Add("Missing IP address");
        }

        // Device age: up to +15 (scaled, max at 365 days)
        if (deviceAgeDays > 0)
        {
            var ageComponent = Math.Min(deviceAgeDays, 365) / 365.0 * 15.0;
            scoreFactors["deviceAge"] = Math.Round(ageComponent, 2);
        }
        else
        {
            scoreFactors["deviceAge"] = 0.0;
            riskIndicators.Add("New or unregistered device");
        }

        var rawScore = 0.0;
        foreach (var factor in scoreFactors.Values)
        {
            rawScore += factor;
        }

        var trustScore = Math.Round(Math.Clamp(rawScore, 0.0, 100.0), 2);

        var trustLevel = trustScore switch
        {
            >= 70.0 => "High",
            >= 40.0 => "Medium",
            _ => "Low"
        };

        var events = new[]
        {
            EngineEvent.Create("DeviceTrustEvaluated", identityGuid,
                new Dictionary<string, object>
                {
                    ["identityId"] = identityId,
                    ["deviceId"] = deviceId,
                    ["trustScore"] = trustScore,
                    ["trustLevel"] = trustLevel,
                    ["evaluatedAt"] = DateTimeOffset.UtcNow.ToString("O"),
                    ["topic"] = "whyce.identity.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["identityId"] = identityId,
            ["deviceId"] = deviceId,
            ["trustScore"] = trustScore,
            ["trustLevel"] = trustLevel,
            ["riskIndicators"] = riskIndicators,
            ["evaluatedAt"] = DateTimeOffset.UtcNow.ToString("O")
        };

        foreach (var kvp in scoreFactors)
        {
            output[$"factor.{kvp.Key}"] = kvp.Value;
        }

        var engineResult = EngineResult.Ok(events, output);
        return IntelligenceResult<EngineResult>.Ok(engineResult,
            IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));
    }

    private static int? ResolveInt(object? value)
    {
        return value switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            decimal m => (int)m,
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }
}
