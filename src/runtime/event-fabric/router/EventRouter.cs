using Whycespace.EventFabric.Models;
using Whycespace.EventFabric.Topics;

namespace Whycespace.EventFabric.Router;

public sealed class EventRouter
{
    private readonly Dictionary<string, List<Func<EventEnvelope, Task>>> _handlers = new();

    public void Register(string topic, Func<EventEnvelope, Task> handler)
    {
        if (!_handlers.TryGetValue(topic, out var list))
        {
            list = new List<Func<EventEnvelope, Task>>();
            _handlers[topic] = list;
        }

        list.Add(handler);
    }

    public async Task RouteAsync(EventEnvelope envelope)
    {
        if (!_handlers.TryGetValue(envelope.Topic, out var handlers))
            return;

        foreach (var handler in handlers)
        {
            await handler(envelope);
        }
    }
}
