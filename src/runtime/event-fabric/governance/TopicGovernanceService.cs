using Whycespace.EventFabric.Registry;

namespace Whycespace.EventFabric.Governance;

public sealed class TopicGovernanceService
{
    private const string TopicPrefix = "whyce.";

    private readonly EventRegistry _eventRegistry;

    public TopicGovernanceService(EventRegistry eventRegistry)
    {
        _eventRegistry = eventRegistry;
    }

    public void ValidateTopicNaming(string topic)
    {
        if (!topic.StartsWith(TopicPrefix, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Topic '{topic}' violates naming convention. All topics must start with '{TopicPrefix}'.");

        if (topic.Contains(' '))
            throw new InvalidOperationException(
                $"Topic '{topic}' violates naming convention. Topics must not contain spaces.");
    }

    public void ValidateClusterOwnership(string eventType, string requestingCluster)
    {
        var owningCluster = _eventRegistry.GetOwningCluster(eventType);

        if (owningCluster is null)
            throw new InvalidOperationException(
                $"Event '{eventType}' is not registered.");

        if (owningCluster != requestingCluster)
            throw new InvalidOperationException(
                $"Cluster '{requestingCluster}' does not own event '{eventType}'. " +
                $"Owned by '{owningCluster}'.");
    }
}
