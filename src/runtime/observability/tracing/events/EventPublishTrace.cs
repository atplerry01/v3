namespace Whycespace.Runtime.Observability.Tracing.Events;

public sealed record EventPublishTrace(
    Guid TraceId,
    Guid EventId,
    string EventType,
    string Topic,
    DateTimeOffset PublishedAt,
    Guid? CorrelationId,
    Guid? CausationId
)
{
    public static EventPublishTrace Create(
        Guid eventId,
        string eventType,
        string topic,
        Guid? correlationId = null,
        Guid? causationId = null) =>
        new(
            TraceId: Guid.NewGuid(),
            EventId: eventId,
            EventType: eventType,
            Topic: topic,
            PublishedAt: DateTimeOffset.UtcNow,
            CorrelationId: correlationId,
            CausationId: causationId
        );
}
