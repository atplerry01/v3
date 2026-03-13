namespace Whycespace.EventFabricRuntime.Routing;

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
public sealed class EventTopicRouter
{
    private readonly Dictionary<string, string> _routes = new();

    public void Register(string eventType, string topic)
    {
        _routes[eventType] = topic;
    }

    public string ResolveTopic(string eventType)
    {
        if (!_routes.TryGetValue(eventType, out var topic))
            throw new InvalidOperationException($"Event route not registered for '{eventType}'.");

        return topic;
    }

    public IReadOnlyDictionary<string, string> GetRoutes()
    {
        return _routes;
    }
}
