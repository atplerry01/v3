using Whycespace.Contracts.Events;

namespace Whycespace.Systems.Downstream.Clusters.Events;

public sealed record ClusterActivatedEvent(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    DateTimeOffset Timestamp,
    string ClusterId,
    string ClusterName,
    string Sector
) : EventBase(EventId, EventType, AggregateId, Timestamp)
{
    public static ClusterActivatedEvent Create(string clusterId, string clusterName, string sector) => new(
        Guid.NewGuid(), "ClusterActivatedEvent", Guid.NewGuid(), DateTimeOffset.UtcNow,
        clusterId, clusterName, sector);
}
