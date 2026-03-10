namespace Whycespace.WorkflowRuntime.Events;

using Whycespace.Contracts.Events;

public sealed record WorkflowStartedEvent(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    DateTimeOffset Timestamp,
    string WorkflowName,
    string WorkflowInstanceId
) : EventBase(EventId, EventType, AggregateId, Timestamp)
{
    public static WorkflowStartedEvent Create(string workflowName, string workflowInstanceId)
        => new(
            Guid.NewGuid(),
            "WorkflowStarted",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            workflowName,
            workflowInstanceId);
}
