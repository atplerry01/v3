using System.Collections.Concurrent;

namespace Whycespace.EventObservability.Failures;

public sealed class EventFailureRecorder
{
    private readonly DeadLetterTracker _deadLetterTracker;
    private readonly ConcurrentBag<EventFailure> _failures = [];

    public EventFailureRecorder(DeadLetterTracker deadLetterTracker)
    {
        _deadLetterTracker = deadLetterTracker;
    }

    public void RecordFailure(Guid eventId, string eventType, string topic, Exception exception)
    {
        var failure = new EventFailure(eventId, eventType, topic, exception.Message, DateTime.UtcNow);
        _failures.Add(failure);

        _deadLetterTracker.Record(eventId, eventType, exception.Message);
    }

    public IReadOnlyList<EventFailure> GetFailures()
    {
        return _failures.ToList();
    }

    public IReadOnlyList<EventFailure> GetFailuresByTopic(string topic)
    {
        return _failures.Where(f => f.Topic == topic).ToList();
    }
}

public sealed record EventFailure(
    Guid EventId,
    string EventType,
    string Topic,
    string ErrorMessage,
    DateTime Timestamp
);
