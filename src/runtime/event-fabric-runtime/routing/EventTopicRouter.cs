namespace Whycespace.EventFabricRuntime.Routing;

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
