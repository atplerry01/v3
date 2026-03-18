using Whycespace.Contracts.Events;

namespace Whycespace.Systems.Downstream.Spv.Events;

public sealed record SpvLifecycleTransitionEvent(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    DateTimeOffset Timestamp,
    string FromState,
    string ToState,
    string Reason
) : EventBase(EventId, EventType, AggregateId, Timestamp)
{
    public static SpvLifecycleTransitionEvent Create(Guid spvId, string fromState, string toState, string reason) => new(
        Guid.NewGuid(), "SpvLifecycleTransitionEvent", spvId, DateTimeOffset.UtcNow,
        fromState, toState, reason);
}
