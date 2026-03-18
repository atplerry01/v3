using Whycespace.Contracts.Events;

namespace Whycespace.Systems.Downstream.Cwg.Events;

public sealed record CwgExecutionFailedEvent(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    DateTimeOffset Timestamp,
    string OperationType,
    string ErrorMessage
) : EventBase(EventId, EventType, AggregateId, Timestamp)
{
    public static CwgExecutionFailedEvent Create(Guid aggregateId, string operationType, string error) => new(
        Guid.NewGuid(), "CwgExecutionFailedEvent", aggregateId, DateTimeOffset.UtcNow,
        operationType, error);
}
