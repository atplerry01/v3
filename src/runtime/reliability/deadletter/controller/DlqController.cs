
using Whycespace.Shared.Primitives.Common;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Events;
using Whycespace.EventFabric.Publisher;
using Whycespace.Reliability.DeadLetter.Models;
using Whycespace.Reliability.Recovery.Engine;

namespace Whycespace.Reliability.DeadLetter.Controller;

public sealed class DlqController
{
    public const string ReplayTopic = "whyce.events.replay";

    private readonly List<DeadLetterEvent> _events = new();
    private readonly Dictionary<Guid, int> _replayCounts = new();
    private readonly EventRecoveryEngine _recoveryEngine;
    private readonly IEventPublisher _publisher;

    public DlqController(EventRecoveryEngine recoveryEngine, IEventPublisher publisher)
    {
        _recoveryEngine = recoveryEngine;
        _publisher = publisher;
    }

    public void Add(DeadLetterEvent deadLetterEvent)
    {
        _events.Add(deadLetterEvent);
    }

    public IReadOnlyList<DeadLetterEvent> GetAll() => _events;

    public DeadLetterEvent? GetById(Guid eventId) =>
        _events.Find(e => e.EventId == eventId);

    public async Task<ReplayResult> ReplayAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var dlqEvent = GetById(eventId);
        if (dlqEvent is null)
            return new ReplayResult(false, "Event not found");

        _replayCounts.TryGetValue(eventId, out var replayCount);
        var decision = _recoveryEngine.Evaluate(dlqEvent, replayCount);

        if (!decision.AllowReplay)
            return new ReplayResult(false, decision.Reason);

        var replayEnvelope = new EventEnvelope(
            EventId: dlqEvent.EventId,
            EventType: dlqEvent.EventType,
            Topic: ReplayTopic,
            Payload: dlqEvent.Payload,
            PartitionKey: new PartitionKey(dlqEvent.EventId.ToString()),
            Timestamp: Timestamp.Now()
        );

        await _publisher.PublishAsync(ReplayTopic, replayEnvelope, cancellationToken);
        _replayCounts[eventId] = replayCount + 1;

        return new ReplayResult(true, "Event published to replay topic");
    }
}

