namespace Whycespace.Platform.RuntimeClient;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Runtime;
using Whycespace.Contracts.Events;

public sealed class EventClient : IEventBus
{
    private readonly IEventBus _eventBus;

    public EventClient(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void Subscribe(string eventType, Func<SystemEvent, Task> handler)
    {
        _eventBus.Subscribe(eventType, handler);
    }

    public Task PublishAsync(SystemEvent @event)
    {
        return _eventBus.PublishAsync(@event);
    }

    public IReadOnlyList<SystemEvent> GetPublishedEvents()
    {
        return _eventBus.GetPublishedEvents();
    }
}
