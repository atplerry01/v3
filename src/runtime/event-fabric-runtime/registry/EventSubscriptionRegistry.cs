namespace Whycespace.EventFabricRuntime.Registry;

public sealed class EventSubscriptionRegistry
{
    private readonly Dictionary<string, List<string>> _subscriptions = new();

    public void Subscribe(string topic, string subscriber)
    {
        if (!_subscriptions.ContainsKey(topic))
            _subscriptions[topic] = new List<string>();

        _subscriptions[topic].Add(subscriber);
    }

    public IReadOnlyList<string> GetSubscribers(string topic)
    {
        if (!_subscriptions.TryGetValue(topic, out var list))
            return Array.Empty<string>();

        return list.AsReadOnly();
    }

    public IReadOnlyDictionary<string, int> GetSubscriptionCounts()
    {
        return _subscriptions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
    }
}
