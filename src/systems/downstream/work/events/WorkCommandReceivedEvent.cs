using Whycespace.Contracts.Events;

namespace Whycespace.Systems.Downstream.Work.Events;

public sealed record WorkCommandReceivedEvent(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    DateTimeOffset Timestamp,
    string CommandType,
    string ClusterId,
    string InitiatorId
) : EventBase(EventId, EventType, AggregateId, Timestamp)
{
    public static WorkCommandReceivedEvent Create(Guid aggregateId, string commandType, string clusterId, string initiatorId) => new(
        Guid.NewGuid(), "WorkCommandReceivedEvent", aggregateId, DateTimeOffset.UtcNow,
        commandType, clusterId, initiatorId);
}
