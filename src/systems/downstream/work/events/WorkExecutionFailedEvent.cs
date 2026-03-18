using Whycespace.Contracts.Events;

namespace Whycespace.Systems.Downstream.Work.Events;

public sealed record WorkExecutionFailedEvent(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    DateTimeOffset Timestamp,
    string ClusterId,
    string WorkerId,
    string TaskType,
    string ErrorMessage
) : EventBase(EventId, EventType, AggregateId, Timestamp)
{
    public static WorkExecutionFailedEvent Create(Guid aggregateId, string clusterId, string workerId, string taskType, string error) => new(
        Guid.NewGuid(), "WorkExecutionFailedEvent", aggregateId, DateTimeOffset.UtcNow,
        clusterId, workerId, taskType, error);
}
