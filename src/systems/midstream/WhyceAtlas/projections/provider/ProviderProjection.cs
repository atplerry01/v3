
using System.Text.Json;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Events;
using Whycespace.ProjectionRuntime.Projections.Contracts;
using Whycespace.ProjectionRuntime.Storage;

namespace Whycespace.Systems.Midstream.WhyceAtlas.Projections;

public sealed class ProviderProjection : IProjection
{
    private readonly IProjectionStore _store;

    public ProviderProjection(IProjectionStore store)
    {
        _store = store;
    }

    public string Name => "ProviderProjection";

    public IReadOnlyCollection<string> EventTypes => ["ProviderRegisteredEvent"];

    public async Task HandleAsync(EventEnvelope envelope)
    {
        if (envelope.EventType != "ProviderRegisteredEvent")
            return;

        var payload = ExtractPayload(envelope.Payload);
        if (payload is null)
            return;

        var providerId = payload.GetValueOrDefault("providerId")?.ToString();
        if (providerId is null)
            return;

        await _store.SetAsync($"provider:{providerId}", JsonSerializer.Serialize(payload));
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
