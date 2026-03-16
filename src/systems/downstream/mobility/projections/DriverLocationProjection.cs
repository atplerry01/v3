using System.Text.Json;
using Whycespace.EventFabric.Models;
using Whycespace.ProjectionRuntime.Projections.Contracts;
using Whycespace.ProjectionRuntime.Storage;

namespace Whycespace.Systems.Downstream.Mobility.Projections;

public sealed class DriverLocationProjection : IProjection
{
    private readonly IProjectionStore _store;

    public DriverLocationProjection(IProjectionStore store)
    {
        _store = store;
    }

    public string Name => "DriverLocationProjection";

    public IReadOnlyCollection<string> EventTypes => ["DriverLocationUpdatedEvent"];

    public async Task HandleAsync(EventEnvelope envelope)
    {
        if (envelope.EventType != "DriverLocationUpdatedEvent")
            return;

        var payload = ExtractPayload(envelope.Payload);
        if (payload is null)
            return;

        var driverId = payload.GetValueOrDefault("driverId")?.ToString();
        if (driverId is null)
            return;

        var lat = Convert.ToDouble(payload.GetValueOrDefault("latitude") ?? 0);
        var lon = Convert.ToDouble(payload.GetValueOrDefault("longitude") ?? 0);

        var model = new { DriverId = driverId, Latitude = lat, Longitude = lon, Timestamp = envelope.Timestamp.Value };

        await _store.SetAsync($"driver:{driverId}", JsonSerializer.Serialize(model));
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
