using System.Text.Json;
using Whycespace.EventFabric.Models;
using Whycespace.Projections.Contracts;
using Whycespace.ProjectionRuntime.Storage;

namespace Whycespace.Projections.Clusters.Property;

public sealed class PropertyListingProjection : IProjection
{
    private readonly IProjectionStore _store;

    public PropertyListingProjection(IProjectionStore store)
    {
        _store = store;
    }

    public string Name => "PropertyListingProjection";

    public IReadOnlyCollection<string> EventTypes =>
    [
        "PropertyListingCreatedEvent",
        "PropertyListingUpdatedEvent"
    ];

    public async Task HandleAsync(EventEnvelope envelope)
    {
        var payload = ExtractPayload(envelope.Payload);
        if (payload is null)
            return;

        var propertyId = payload.GetValueOrDefault("propertyId")?.ToString();
        if (propertyId is null)
            return;

        var address = payload.GetValueOrDefault("address")?.ToString() ?? "";

        var status = envelope.EventType switch
        {
            "PropertyListingCreatedEvent" => "Active",
            "PropertyListingUpdatedEvent" => "Updated",
            _ => "Unknown"
        };

        var model = new { PropertyId = propertyId, Address = address, Status = status };

        await _store.SetAsync($"property:{propertyId}", JsonSerializer.Serialize(model));
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
