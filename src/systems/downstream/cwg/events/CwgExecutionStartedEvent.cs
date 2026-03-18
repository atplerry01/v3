using Whycespace.Contracts.Events;

namespace Whycespace.Systems.Downstream.Cwg.Events;

public sealed record CwgExecutionStartedEvent(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    DateTimeOffset Timestamp,
    string OperationType,
    Guid InitiatorId
) : EventBase(EventId, EventType, AggregateId, Timestamp)
{
    public static CwgExecutionStartedEvent Create(Guid aggregateId, string operationType, Guid initiatorId) => new(
        Guid.NewGuid(), "CwgExecutionStartedEvent", aggregateId, DateTimeOffset.UtcNow,
        operationType, initiatorId);
}
