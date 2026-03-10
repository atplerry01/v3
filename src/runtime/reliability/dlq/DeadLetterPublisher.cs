using Whycespace.Contracts.Primitives;
using Whycespace.EventFabric.Models;
using Whycespace.EventFabric.Publisher;

namespace Whycespace.Reliability.Dlq;

public sealed class DeadLetterPublisher
{
    public const string DlqTopic = "whyce.dlq.events";

    private readonly IEventPublisher _publisher;

    public DeadLetterPublisher(IEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task PublishAsync(
        EventEnvelope envelope,
        string reason,
        CancellationToken cancellationToken)
    {
        var dlqEnvelope = new EventEnvelope(
            Guid.NewGuid(),
            $"DeadLetter:{envelope.EventType}",
            DlqTopic,
            new { OriginalEvent = envelope, Reason = reason },
            envelope.PartitionKey,
            Timestamp.Now()
        );

        return _publisher.PublishAsync(DlqTopic, dlqEnvelope, cancellationToken);
    }
}
