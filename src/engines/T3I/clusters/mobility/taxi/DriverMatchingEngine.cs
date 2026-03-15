namespace Whycespace.Engines.T3I.Clusters.Mobility.Taxi;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("DriverMatching", EngineTier.T3I, EngineKind.Decision, "DriverMatchingRequest", typeof(EngineEvent))]
public sealed class DriverMatchingEngine : IEngine
{
    public string Name => "DriverMatching";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var pickupLat = context.Data.GetValueOrDefault("pickupLatitude");
        var pickupLon = context.Data.GetValueOrDefault("pickupLongitude");

        if (pickupLat is null || pickupLon is null)
            return Task.FromResult(EngineResult.Fail("Missing pickup coordinates"));

        var matchedDriverId = Guid.NewGuid().ToString();

        var events = new[]
        {
            EngineEvent.Create("DriverMatched", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["matchedDriverId"] = matchedDriverId,
                    ["pickupLatitude"] = pickupLat,
                    ["pickupLongitude"] = pickupLon
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object> { ["assignedDriverId"] = matchedDriverId }));
    }
}
