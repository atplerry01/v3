using Whycespace.Contracts.Events;

namespace Whycespace.Systems.Downstream.Spv.Events;

public sealed record SpvCreatedEvent(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    DateTimeOffset Timestamp,
    string Name,
    string ClusterId,
    decimal AllocatedCapital
) : EventBase(EventId, EventType, AggregateId, Timestamp)
{
    public static SpvCreatedEvent Create(Guid spvId, string name, string clusterId, decimal allocatedCapital) => new(
        Guid.NewGuid(), "SpvCreatedEvent", spvId, DateTimeOffset.UtcNow,
        name, clusterId, allocatedCapital);
}
