namespace Whycespace.Engines.T3I.Clusters.Mobility.Taxi;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("RideCreation", EngineTier.T3I, EngineKind.Decision, "RideCreationRequest", typeof(EngineEvent))]
public sealed class RideCreationEngine : IEngine
{
    public string Name => "RideCreation";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var riderId = context.Data.GetValueOrDefault("riderId") as string;
        if (string.IsNullOrEmpty(riderId))
            return Task.FromResult(EngineResult.Fail("Missing riderId"));

        var driverId = context.Data.GetValueOrDefault("assignedDriverId") as string;
        if (string.IsNullOrEmpty(driverId))
            return Task.FromResult(EngineResult.Fail("Missing assignedDriverId"));

        var rideId = Guid.NewGuid();

        var events = new[]
        {
            EngineEvent.Create("RideCreated", rideId,
                new Dictionary<string, object>
                {
                    ["rideId"] = rideId.ToString(),
                    ["driverId"] = driverId,
                    ["riderId"] = riderId,
                    ["topic"] = "whyce.mobility.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["rideId"] = rideId.ToString(),
                ["driverId"] = driverId,
                ["riderId"] = riderId
            }));
    }
}
