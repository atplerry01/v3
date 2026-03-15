namespace Whycespace.WorkflowRuntime;

public sealed record WorkflowEventRouteCommand(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    string? WorkflowCorrelationId,
    IReadOnlyDictionary<string, object> Payload,
    DateTimeOffset Timestamp
)
{
    public static WorkflowEventRouteCommand Create(
        string eventType,
        Guid aggregateId,
        string? workflowCorrelationId = null,
        IReadOnlyDictionary<string, object>? payload = null)
        => new(
            Guid.NewGuid(),
            eventType,
            aggregateId,
            workflowCorrelationId,
            payload ?? new Dictionary<string, object>(),
            DateTimeOffset.UtcNow);
}
