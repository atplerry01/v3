namespace Whycespace.WorkflowRuntime.Events;

using Whycespace.Contracts.Events;

public sealed record WorkflowCompletedEvent(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    DateTimeOffset Timestamp,
    string WorkflowName,
    string WorkflowInstanceId,
    bool Success
) : EventBase(EventId, EventType, AggregateId, Timestamp)
{
    public static WorkflowCompletedEvent Create(string workflowName, string workflowInstanceId, bool success)
        => new(
            Guid.NewGuid(),
            success ? "WorkflowCompleted" : "WorkflowFailed",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            workflowName,
            workflowInstanceId,
            success);
}
