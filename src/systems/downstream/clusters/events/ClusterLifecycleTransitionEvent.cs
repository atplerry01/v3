using Whycespace.Contracts.Events;

namespace Whycespace.Systems.Downstream.Clusters.Events;

public sealed record ClusterLifecycleTransitionEvent(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    DateTimeOffset Timestamp,
    string ClusterId,
    string FromState,
    string ToState
) : EventBase(EventId, EventType, AggregateId, Timestamp)
{
    public static ClusterLifecycleTransitionEvent Create(string clusterId, string fromState, string toState) => new(
        Guid.NewGuid(), "ClusterLifecycleTransitionEvent", Guid.NewGuid(), DateTimeOffset.UtcNow,
        clusterId, fromState, toState);
}
