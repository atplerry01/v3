using Whycespace.Engines.T3I.Atlas.Identity.Models;
namespace Whycespace.Engines.T3I.Atlas.Identity.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Engines.T3I.Shared;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("IdentityGraph", EngineTier.T3I, EngineKind.Decision, "AnalyzeIdentityGraphCommand", typeof(EngineEvent))]
public sealed class IdentityGraphEngine : IEngine, IIntelligenceEngine<EngineContext, EngineResult>
{
    public string Name => "IdentityGraph";
    public string EngineName => "IdentityGraph";

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

        var connectedDevices = ResolveStringList(engineContext.Data.GetValueOrDefault("connectedDevices"));
        var connectedProviders = ResolveGuidList(engineContext.Data.GetValueOrDefault("connectedProviders"));
        var connectedOperators = ResolveGuidList(engineContext.Data.GetValueOrDefault("connectedOperators"));
        var connectedServices = ResolveGuidList(engineContext.Data.GetValueOrDefault("connectedServices"));

        // Build graph metrics
        var totalConnections = connectedDevices.Count + connectedProviders.Count
                             + connectedOperators.Count + connectedServices.Count;

        var sharedDeviceCount = connectedDevices.Count;

        // Analyze suspicious connections
        var suspiciousConnections = 0;
        var riskComponents = new Dictionary<string, double>();

        // Identity relationship density: high device sharing is suspicious
        // More than 3 devices sharing = suspicious signal
        if (sharedDeviceCount > 3)
        {
            var deviceRisk = Math.Min((sharedDeviceCount - 3) * 5.0, 25.0);
            riskComponents["deviceSharing"] = Math.Round(deviceRisk, 2);
            suspiciousConnections += sharedDeviceCount - 3;
        }

        // Provider network linkage: excessive provider overlap
        // More than 5 providers = suspicious
        if (connectedProviders.Count > 5)
        {
            var providerRisk = Math.Min((connectedProviders.Count - 5) * 4.0, 20.0);
            riskComponents["providerOverlap"] = Math.Round(providerRisk, 2);
            suspiciousConnections += connectedProviders.Count - 5;
        }

        // Operator network clustering: excessive operator connections
        // More than 3 operators = suspicious
        if (connectedOperators.Count > 3)
        {
            var operatorRisk = Math.Min((connectedOperators.Count - 3) * 6.0, 30.0);
            riskComponents["operatorClustering"] = Math.Round(operatorRisk, 2);
            suspiciousConnections += connectedOperators.Count - 3;
        }

        // Service connection density
        // More than 10 services = suspicious
        if (connectedServices.Count > 10)
        {
            var serviceRisk = Math.Min((connectedServices.Count - 10) * 2.5, 25.0);
            riskComponents["serviceDensity"] = Math.Round(serviceRisk, 2);
            suspiciousConnections += connectedServices.Count - 10;
        }

        // Compute total risk score (0-100)
        var rawRisk = 0.0;
        foreach (var component in riskComponents.Values)
        {
            rawRisk += component;
        }
        var riskScore = Math.Round(Math.Clamp(rawRisk, 0.0, 100.0), 2);

        // Connected identity count is approximated by unique connections
        var connectedIdentityCount = totalConnections;

        var events = new[]
        {
            EngineEvent.Create("IdentityGraphAnalyzed", identityGuid,
                new Dictionary<string, object>
                {
                    ["identityId"] = identityId,
                    ["riskScore"] = riskScore,
                    ["suspiciousConnections"] = suspiciousConnections,
                    ["analyzedAt"] = DateTimeOffset.UtcNow.ToString("O"),
                    ["topic"] = "whyce.identity.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["identityId"] = identityId,
            ["connectedIdentityCount"] = connectedIdentityCount,
            ["sharedDeviceCount"] = sharedDeviceCount,
            ["suspiciousConnections"] = suspiciousConnections,
            ["riskScore"] = riskScore,
            ["analyzedAt"] = DateTimeOffset.UtcNow.ToString("O")
        };

        foreach (var kvp in riskComponents)
        {
            output[$"factor.{kvp.Key}"] = kvp.Value;
        }

        var engineResult = EngineResult.Ok(events, output);
        return IntelligenceResult<EngineResult>.Ok(engineResult,
            IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));
    }

    private static List<string> ResolveStringList(object? value)
    {
        return value switch
        {
            List<string> list => list,
            IEnumerable<object> enumerable => enumerable.Select(o => o?.ToString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList(),
            string s when !string.IsNullOrEmpty(s) => s.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList(),
            _ => new List<string>()
        };
    }

    private static List<Guid> ResolveGuidList(object? value)
    {
        return value switch
        {
            List<Guid> list => list,
            IEnumerable<object> enumerable => enumerable
                .Select(o => o?.ToString() ?? "")
                .Where(s => Guid.TryParse(s, out _))
                .Select(Guid.Parse)
                .ToList(),
            string s when !string.IsNullOrEmpty(s) => s.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => Guid.TryParse(x, out _))
                .Select(Guid.Parse)
                .ToList(),
            _ => new List<Guid>()
        };
    }
}
