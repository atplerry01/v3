namespace Whycespace.Contracts.Events;

public sealed record SystemEvent(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, object> Payload
) : EventBase(EventId, EventType, AggregateId, Timestamp)
{
    public static SystemEvent Create(string eventType, Guid aggregateId, IReadOnlyDictionary<string, object>? payload = null)
        => new(Guid.NewGuid(), eventType, aggregateId, DateTimeOffset.UtcNow, payload ?? new Dictionary<string, object>());
}
