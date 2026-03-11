namespace Whycespace.EventFabricRuntime.Registry;

/// <summary>
/// Thread Safety Notice
/// --------------------
/// This component is designed for single-threaded runtime access.
///
/// In the Whycespace runtime architecture, execution is serialized
/// through partition workers and workflow dispatchers.
///
/// Because of this guarantee, concurrent collections are not required
/// and standard Dictionary/List structures are used for efficiency.
///
/// If this component is used outside the partition runtime context,
/// external synchronization must be applied.
/// </summary>
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
