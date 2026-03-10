namespace Whycespace.Shared.Contracts;

using Whycespace.Shared.Events;

public interface IEventBus
{
    void Subscribe(string eventType, Func<SystemEvent, Task> handler);
    Task PublishAsync(SystemEvent @event);
    IReadOnlyList<SystemEvent> GetPublishedEvents();
}
