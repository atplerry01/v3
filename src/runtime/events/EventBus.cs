namespace Whycespace.Runtime.Events;

using Whycespace.Shared.Events;

public sealed class EventBus
{
    private readonly List<SystemEvent> _events = new();
    private readonly Dictionary<string, List<Func<SystemEvent, Task>>> _handlers = new();

    public void Subscribe(string eventType, Func<SystemEvent, Task> handler)
    {
        if (!_handlers.ContainsKey(eventType))
            _handlers[eventType] = new List<Func<SystemEvent, Task>>();
        _handlers[eventType].Add(handler);
    }

    public async Task PublishAsync(SystemEvent @event)
    {
        _events.Add(@event);

        if (_handlers.TryGetValue(@event.EventType, out var handlers))
        {
            foreach (var handler in handlers)
                await handler(@event);
        }
    }

    public IReadOnlyList<SystemEvent> GetPublishedEvents() => _events;
}
