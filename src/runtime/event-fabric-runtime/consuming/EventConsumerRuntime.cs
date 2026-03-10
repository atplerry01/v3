namespace Whycespace.EventFabricRuntime.Consuming;

using Whycespace.EventFabricRuntime.Registry;

public sealed class EventConsumerRuntime
{
    private readonly EventSubscriptionRegistry _registry;

    public EventConsumerRuntime(EventSubscriptionRegistry registry)
    {
        _registry = registry;
    }

    public IReadOnlyList<string> GetSubscribers(string topic)
    {
        return _registry.GetSubscribers(topic);
    }
}
