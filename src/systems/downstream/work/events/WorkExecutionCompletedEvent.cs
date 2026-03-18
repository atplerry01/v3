using Whycespace.Contracts.Events;

namespace Whycespace.Systems.Downstream.Work.Events;

public sealed record WorkExecutionCompletedEvent(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    DateTimeOffset Timestamp,
    string ClusterId,
    string WorkerId,
    string TaskType,
    string Result
) : EventBase(EventId, EventType, AggregateId, Timestamp)
{
    public static WorkExecutionCompletedEvent Create(Guid aggregateId, string clusterId, string workerId, string taskType, string result) => new(
        Guid.NewGuid(), "WorkExecutionCompletedEvent", aggregateId, DateTimeOffset.UtcNow,
        clusterId, workerId, taskType, result);
}
