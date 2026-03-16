using System.Text.Json;
using Whycespace.EventFabric.Models;
using Whycespace.ProjectionRuntime.Projections.Contracts;
using Whycespace.ProjectionRuntime.Storage;

namespace Whycespace.Systems.Downstream.Mobility.Projections;

public sealed class RideStatusProjection : IProjection
{
    private readonly IProjectionStore _store;

    public RideStatusProjection(IProjectionStore store)
    {
        _store = store;
    }

    public string Name => "RideStatusProjection";

    public IReadOnlyCollection<string> EventTypes =>
    [
        "RideCreatedEvent",
        "RideCompletedEvent",
        "RideCancelledEvent"
    ];

    public async Task HandleAsync(EventEnvelope envelope)
    {
        var payload = ExtractPayload(envelope.Payload);
        if (payload is null)
            return;

        var rideId = payload.GetValueOrDefault("rideId")?.ToString();
        if (rideId is null)
            return;

        var driverId = payload.GetValueOrDefault("driverId")?.ToString() ?? "";
        var passengerId = payload.GetValueOrDefault("passengerId")?.ToString() ?? "";

        var status = envelope.EventType switch
        {
            "RideCreatedEvent" => "Created",
            "RideCompletedEvent" => "Completed",
            "RideCancelledEvent" => "Cancelled",
            _ => "Unknown"
        };

        var model = new { RideId = rideId, DriverId = driverId, PassengerId = passengerId, Status = status };

        await _store.SetAsync($"ride:{rideId}", JsonSerializer.Serialize(model));
    }

    private static Dictionary<string, object>? ExtractPayload(object payload)
    {
        if (payload is Dictionary<string, object> dict)
            return dict;

        if (payload is JsonElement element)
            return JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());

        return null;
    }
}
