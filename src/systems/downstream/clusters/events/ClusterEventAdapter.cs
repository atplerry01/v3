using Whycespace.Contracts.Events;
using Whycespace.Contracts.Runtime;

namespace Whycespace.Systems.Downstream.Clusters.Events;

public sealed class ClusterEventAdapter
{
    private readonly IEventBus _eventBus;

    public ClusterEventAdapter(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task PublishClusterActivatedAsync(string clusterId, string clusterName, string sector)
    {
        var @event = SystemEvent.Create("ClusterActivatedEvent", Guid.NewGuid(), new Dictionary<string, object>
        {
            ["clusterId"] = clusterId,
            ["clusterName"] = clusterName,
            ["sector"] = sector
        });
        await _eventBus.PublishAsync(@event);
    }

    public async Task PublishProviderRegisteredAsync(Guid providerId, string clusterId, string subClusterId, string providerType)
    {
        var @event = SystemEvent.Create("ProviderRegisteredEvent", providerId, new Dictionary<string, object>
        {
            ["providerId"] = providerId.ToString(),
            ["clusterId"] = clusterId,
            ["subClusterId"] = subClusterId,
            ["providerType"] = providerType
        });
        await _eventBus.PublishAsync(@event);
    }

    public async Task PublishLifecycleTransitionAsync(string clusterId, string fromState, string toState)
    {
        var @event = SystemEvent.Create("ClusterLifecycleTransitionEvent", Guid.NewGuid(), new Dictionary<string, object>
        {
            ["clusterId"] = clusterId,
            ["fromState"] = fromState,
            ["toState"] = toState
        });
        await _eventBus.PublishAsync(@event);
    }
}
