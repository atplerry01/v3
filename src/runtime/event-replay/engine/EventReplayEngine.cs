
using Whycespace.Contracts.Events;
using Whycespace.Shared.Envelopes;
using Whycespace.EventFabric.Router;
using Whycespace.EventReplay.Models;

namespace Whycespace.EventReplay.Engine;

public sealed class EventReplayEngine
{
    private readonly EventRouter _router;
    private readonly ReplayStatus _status = new();

    public EventReplayEngine(EventRouter router)
    {
        _router = router;
    }

    public ReplayStatus Status => _status;

    public async Task ReplayEventsAsync(
        IReadOnlyList<EventEnvelope> events,
        CancellationToken cancellationToken)
    {
        _status.Replaying = true;
        _status.ProcessedEvents = 0;
        _status.StartedAt = DateTime.UtcNow;
        _status.CompletedAt = null;

        foreach (var envelope in events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _router.RouteAsync(envelope);
            _status.ProcessedEvents++;
        }

        _status.Replaying = false;
        _status.CompletedAt = DateTime.UtcNow;
    }

    public async Task ReplayTopicAsync(
        string topic,
        IReadOnlyList<EventEnvelope> events,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        _status.CurrentTopic = topic;

        var filtered = events
            .Where(e => e.Topic == topic)
            .Where(e => e.Timestamp.Value >= from && e.Timestamp.Value <= to)
            .ToList();

        await ReplayEventsAsync(filtered, cancellationToken);

        _status.CurrentTopic = null;
    }
}
