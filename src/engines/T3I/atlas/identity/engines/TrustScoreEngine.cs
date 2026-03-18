using Whycespace.Engines.T3I.Atlas.Identity.Models;
namespace Whycespace.Engines.T3I.Atlas.Identity.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Engines.T3I.Shared;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("TrustScore", EngineTier.T3I, EngineKind.Decision, "EvaluateTrustScoreCommand", typeof(EngineEvent))]
public sealed class TrustScoreEngine : IEngine, IIntelligenceEngine<EngineContext, EngineResult>
{
    public string Name => "TrustScore";
    public string EngineName => "TrustScore";

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

        var verifiedEmail = ResolveBool(engineContext.Data.GetValueOrDefault("verifiedEmail"));
        var verifiedPhone = ResolveBool(engineContext.Data.GetValueOrDefault("verifiedPhone"));
        var verifiedDocuments = ResolveBool(engineContext.Data.GetValueOrDefault("verifiedDocuments"));
        var deviceTrustScore = ResolveDouble(engineContext.Data.GetValueOrDefault("deviceTrustScore")) ?? 0.0;
        var accountAgeDays = ResolveInt(engineContext.Data.GetValueOrDefault("accountAgeDays")) ?? 0;
        var behaviorScore = ResolveDouble(engineContext.Data.GetValueOrDefault("behaviorScore")) ?? 0.0;

        var scoreFactors = new Dictionary<string, double>();

        // Email Verification: +10
        if (verifiedEmail)
        {
            scoreFactors["emailVerification"] = 10.0;
        }

        // Phone Verification: +10
        if (verifiedPhone)
        {
            scoreFactors["phoneVerification"] = 10.0;
        }

        // Document Verification: +20
        if (verifiedDocuments)
        {
            scoreFactors["documentVerification"] = 20.0;
        }

        // Device Trust: up to +15 (scaled by deviceTrustScore 0.0-1.0)
        var deviceComponent = Math.Clamp(deviceTrustScore, 0.0, 1.0) * 15.0;
        scoreFactors["deviceTrust"] = Math.Round(deviceComponent, 2);

        // Account Age: up to +10 (scaled, max at 365 days)
        var ageComponent = Math.Min(accountAgeDays, 365) / 365.0 * 10.0;
        scoreFactors["accountAge"] = Math.Round(ageComponent, 2);

        // Behavior Score: up to +20 (scaled by behaviorScore 0.0-1.0)
        var behaviorComponent = Math.Clamp(behaviorScore, 0.0, 1.0) * 20.0;
        scoreFactors["behaviorScore"] = Math.Round(behaviorComponent, 2);

        // Reserved capacity: up to +15 for future factors
        // Maximum possible: 10 + 10 + 20 + 15 + 10 + 20 + 15 = 100

        var rawScore = 0.0;
        foreach (var factor in scoreFactors.Values)
        {
            rawScore += factor;
        }

        // Normalize between 0 and 100
        var trustScore = Math.Round(Math.Clamp(rawScore, 0.0, 100.0), 2);

        var events = new[]
        {
            EngineEvent.Create("TrustScoreEvaluated", identityGuid,
                new Dictionary<string, object>
                {
                    ["identityId"] = identityId,
                    ["trustScore"] = trustScore,
                    ["evaluatedAt"] = DateTimeOffset.UtcNow.ToString("O"),
                    ["topic"] = "whyce.identity.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["identityId"] = identityId,
            ["trustScore"] = trustScore,
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

    private static bool ResolveBool(object? value)
    {
        return value switch
        {
            bool b => b,
            string s => s.Equals("true", StringComparison.OrdinalIgnoreCase),
            int i => i != 0,
            _ => false
        };
    }

    private static double? ResolveDouble(object? value)
    {
        return value switch
        {
            double d => d,
            decimal m => (double)m,
            int i => i,
            long l => l,
            string s when double.TryParse(s, out var parsed) => parsed,
            _ => null
        };
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
