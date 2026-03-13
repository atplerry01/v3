namespace Whycespace.Engines.T3I.Clusters.Mobility.Taxi;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("RideCompletion", EngineTier.T3I, EngineKind.Decision, "RideCompletionRequest", typeof(EngineEvent))]
public sealed class RideCompletionEngine : IEngine
{
    public string Name => "RideCompletion";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var rideId = context.Data.GetValueOrDefault("rideId") as string;
        if (string.IsNullOrEmpty(rideId))
            return Task.FromResult(EngineResult.Fail("Missing rideId"));

        if (!Guid.TryParse(rideId, out var rideGuid))
            return Task.FromResult(EngineResult.Fail("Invalid rideId format"));

        var driverId = context.Data.GetValueOrDefault("driverId") as string ?? "";
        var riderId = context.Data.GetValueOrDefault("riderId") as string ?? "";
        var fare = ResolveDecimal(context.Data.GetValueOrDefault("fare"), 0m);

        if (fare <= 0)
            return Task.FromResult(EngineResult.Fail("Fare must be greater than zero"));

        var events = new[]
        {
            EngineEvent.Create("RideCompleted", rideGuid,
                new Dictionary<string, object>
                {
                    ["rideId"] = rideId,
                    ["driverId"] = driverId,
                    ["riderId"] = riderId,
                    ["fare"] = fare,
                    ["topic"] = "whyce.mobility.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["rideId"] = rideId,
                ["fare"] = fare,
                ["status"] = "Completed"
            }));
    }

    private static decimal ResolveDecimal(object? value, decimal fallback)
    {
        return value switch
        {
            decimal d => d,
            double d => (decimal)d,
            int i => i,
            long l => l,
            string s when decimal.TryParse(s, out var parsed) => parsed,
            _ => fallback
        };
    }
}
