using Whycespace.Contracts.Events;

namespace Whycespace.Systems.Downstream.Clusters.Events;

public sealed record ProviderRegisteredEvent(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    DateTimeOffset Timestamp,
    Guid ProviderId,
    string ClusterId,
    string SubClusterId,
    string ProviderType
) : EventBase(EventId, EventType, AggregateId, Timestamp)
{
    public static ProviderRegisteredEvent Create(Guid providerId, string clusterId, string subClusterId, string providerType) => new(
        Guid.NewGuid(), "ProviderRegisteredEvent", providerId, DateTimeOffset.UtcNow,
        providerId, clusterId, subClusterId, providerType);
}
