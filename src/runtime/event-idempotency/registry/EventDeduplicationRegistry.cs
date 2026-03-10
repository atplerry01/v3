using Whycespace.EventIdempotency.Models;

namespace Whycespace.EventIdempotency.Registry;

public sealed class EventDeduplicationRegistry
{
    private readonly Dictionary<Guid, ProcessedEvent> _processed = new();

    public bool HasProcessed(Guid eventId)
    {
        return _processed.ContainsKey(eventId);
    }

    public void MarkProcessed(ProcessedEvent entry)
    {
        _processed.TryAdd(entry.EventId, entry);
    }

    public int ProcessedCount => _processed.Count;

    public ProcessedEvent? GetProcessedEvent(Guid eventId)
    {
        _processed.TryGetValue(eventId, out var entry);
        return entry;
    }
}
