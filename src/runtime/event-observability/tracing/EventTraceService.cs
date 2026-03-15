using System.Collections.Concurrent;

namespace Whycespace.EventObservability.Tracing;

public sealed class EventTraceService
{
    private readonly ConcurrentDictionary<Guid, EventTrace> _traces = new();
    private readonly ConcurrentDictionary<Guid, List<Guid>> _correlationIndex = new();
    private readonly ConcurrentDictionary<Guid, List<Guid>> _causationIndex = new();

    public EventTrace StartTrace(Guid eventId, string eventType, Guid? correlationId = null, Guid? causationId = null)
    {
        var traceId = Guid.NewGuid();
        var resolvedCorrelationId = correlationId ?? traceId;
        var resolvedCausationId = causationId ?? eventId;

        var trace = new EventTrace(
            traceId,
            eventId,
            eventType,
            resolvedCorrelationId,
            resolvedCausationId,
            DateTime.UtcNow
        );

        _traces[eventId] = trace;

        _correlationIndex.AddOrUpdate(
            resolvedCorrelationId,
            [eventId],
            (_, list) => { list.Add(eventId); return list; }
        );

        _causationIndex.AddOrUpdate(
            resolvedCausationId,
            [eventId],
            (_, list) => { list.Add(eventId); return list; }
        );

        return trace;
    }

    public EventTrace? GetTrace(Guid eventId)
    {
        return _traces.GetValueOrDefault(eventId);
    }

    public IReadOnlyList<EventTrace> GetCorrelatedEvents(Guid correlationId)
    {
        if (!_correlationIndex.TryGetValue(correlationId, out var eventIds))
            return [];

        return eventIds
            .Select(id => _traces.GetValueOrDefault(id))
            .Where(t => t is not null)
            .Cast<EventTrace>()
            .ToList();
    }

    public IReadOnlyList<EventTrace> GetCausedEvents(Guid causationId)
    {
        if (!_causationIndex.TryGetValue(causationId, out var eventIds))
            return [];

        return eventIds
            .Select(id => _traces.GetValueOrDefault(id))
            .Where(t => t is not null)
            .Cast<EventTrace>()
            .ToList();
    }

    public IReadOnlyList<EventTrace> GetEventChain(Guid correlationId)
    {
        return GetCorrelatedEvents(correlationId)
            .OrderBy(t => t.Timestamp)
            .ToList();
    }
}

public sealed record EventTrace(
    Guid TraceId,
    Guid EventId,
    string EventType,
    Guid CorrelationId,
    Guid CausationId,
    DateTime Timestamp
);
