using System.Collections.Concurrent;
using Whycespace.EventObservability.Metrics;

namespace Whycespace.EventObservability.Failures;

public sealed class DeadLetterTracker
{
    private readonly EventMetrics _metrics;
    private readonly ConcurrentBag<DeadLetterRecord> _records = [];

    public DeadLetterTracker(EventMetrics metrics)
    {
        _metrics = metrics;
    }

    public void Record(Guid eventId, string eventType, string failureReason)
    {
        var record = new DeadLetterRecord(eventId, eventType, failureReason, DateTime.UtcNow);
        _records.Add(record);
        _metrics.RecordDeadLetter();
    }

    public IReadOnlyList<DeadLetterRecord> GetRecords()
    {
        return _records.ToList();
    }

    public IReadOnlyList<DeadLetterRecord> GetRecordsSince(DateTime since)
    {
        return _records.Where(r => r.Timestamp >= since).ToList();
    }

    public int GetCount()
    {
        return _records.Count;
    }
}

public sealed record DeadLetterRecord(
    Guid EventId,
    string EventType,
    string FailureReason,
    DateTime Timestamp
);
