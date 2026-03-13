using System.Text.Json;
using Whycespace.EventFabric.Models;
using Whycespace.Projections.Contracts;
using Whycespace.ProjectionRuntime.Storage;

namespace Whycespace.Projections.Core.Economics;

public sealed class RevenueProjection : IProjection
{
    private readonly IProjectionStore _store;

    public RevenueProjection(IProjectionStore store)
    {
        _store = store;
    }

    public string Name => "RevenueProjection";

    public IReadOnlyCollection<string> EventTypes => ["RevenueRecordedEvent"];

    public async Task HandleAsync(EventEnvelope envelope)
    {
        if (envelope.EventType != "RevenueRecordedEvent")
            return;

        var payload = ExtractPayload(envelope.Payload);
        if (payload is null)
            return;

        var aggregateId = payload.GetValueOrDefault("aggregateId")?.ToString();
        if (aggregateId is null)
            return;

        var amount = Convert.ToDecimal(payload.GetValueOrDefault("amount") ?? 0);

        var existing = await _store.GetAsync($"revenue:{aggregateId}");
        var current = existing is not null
            ? JsonSerializer.Deserialize<RevenueState>(existing)?.Revenue ?? 0m
            : 0m;

        var model = new RevenueState(aggregateId, current + amount);

        await _store.SetAsync($"revenue:{aggregateId}", JsonSerializer.Serialize(model));
    }

    private static Dictionary<string, object>? ExtractPayload(object payload)
    {
        if (payload is Dictionary<string, object> dict)
            return dict;

        if (payload is JsonElement element)
            return JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());

        return null;
    }

    private sealed record RevenueState(string AggregateId, decimal Revenue);
}
