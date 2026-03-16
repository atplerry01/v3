namespace Whycespace.Systems.Upstream.WhyceChain.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Upstream.WhyceChain.Models;

public sealed class ChainEventStore
{
    private readonly ConcurrentDictionary<string, ChainEvent> _events = new();

    public void AddEvent(ChainEvent chainEvent)
    {
        if (!_events.TryAdd(chainEvent.EventId, chainEvent))
            throw new InvalidOperationException($"Duplicate event: {chainEvent.EventId}");
    }

    public ChainEvent GetEvent(string eventId)
    {
        if (!_events.TryGetValue(eventId, out var chainEvent))
            throw new KeyNotFoundException($"Chain event not found: {eventId}");

        return chainEvent;
    }

    public IReadOnlyCollection<ChainEvent> GetAllEvents()
    {
        return _events.Values.ToList();
    }

    public IReadOnlyCollection<ChainEvent> GetEventsByDomain(string domain)
    {
        return _events.Values
            .Where(e => e.Domain == domain)
            .ToList();
    }
}
