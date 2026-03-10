namespace Whycespace.Contracts.Engines;

public sealed record EngineEvent(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, object> Payload
)
{
    public static EngineEvent Create(string eventType, Guid aggregateId, IReadOnlyDictionary<string, object>? payload = null)
        => new(Guid.NewGuid(), eventType, aggregateId, DateTimeOffset.UtcNow, payload ?? new Dictionary<string, object>());
}
