using System.Text.Json;
using Whycespace.Contracts.Primitives;
using Whycespace.EventFabric.Models;
using Whycespace.EventFabric.Publisher;
using Whycespace.Reliability.DeadLetter.Models;

namespace Whycespace.Reliability.DeadLetter.Publisher;

public sealed class DeadLetterEventPublisher
{
    public const string DlqTopic = "whyce.events.deadletter";

    private readonly IEventPublisher _publisher;

    public DeadLetterEventPublisher(IEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task PublishAsync(DeadLetterEvent deadLetterEvent, CancellationToken cancellationToken = default)
    {
        var envelope = new EventEnvelope(
            EventId: deadLetterEvent.EventId,
            EventType: $"DeadLetter:{deadLetterEvent.EventType}",
            Topic: DlqTopic,
            Payload: JsonSerializer.Serialize(deadLetterEvent),
            PartitionKey: deadLetterEvent.EventId.ToString(),
            Timestamp: Timestamp.Now()
        );

        return _publisher.PublishAsync(DlqTopic, envelope, cancellationToken);
    }
}
