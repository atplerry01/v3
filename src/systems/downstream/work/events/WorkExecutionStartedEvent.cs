using Whycespace.Contracts.Events;

namespace Whycespace.Systems.Downstream.Work.Events;

public sealed record WorkExecutionStartedEvent(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    DateTimeOffset Timestamp,
    string ClusterId,
    string SubClusterId,
    string WorkerId,
    string TaskType
) : EventBase(EventId, EventType, AggregateId, Timestamp)
{
    public static WorkExecutionStartedEvent Create(Guid aggregateId, string clusterId, string subClusterId, string workerId, string taskType) => new(
        Guid.NewGuid(), "WorkExecutionStartedEvent", aggregateId, DateTimeOffset.UtcNow,
        clusterId, subClusterId, workerId, taskType);
}
