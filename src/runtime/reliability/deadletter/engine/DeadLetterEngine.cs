using System.Text.Json;
using Whycespace.EventFabric.Models;
using Whycespace.Reliability.DeadLetter.Models;

namespace Whycespace.Reliability.DeadLetter.Engine;

public sealed class DeadLetterEngine
{
    public DeadLetterEvent CreateDeadLetterEvent(
        EventEnvelope envelope,
        DeadLetterReason reason,
        string errorMessage,
        int retryCount)
    {
        return new DeadLetterEvent(
            EventId: envelope.EventId,
            EventType: envelope.EventType,
            SourceTopic: envelope.Topic,
            Partition: 0,
            Offset: envelope.SequenceNumber,
            Reason: reason,
            ErrorMessage: errorMessage,
            RetryCount: retryCount,
            FailedAt: DateTime.UtcNow,
            Payload: SerializePayload(envelope.Payload)
        );
    }

    public DeadLetterMetadata CreateMetadata(
        DeadLetterEvent deadLetterEvent,
        DateTime? firstFailure = null)
    {
        return new DeadLetterMetadata(
            EventId: deadLetterEvent.EventId,
            RetryCount: deadLetterEvent.RetryCount,
            FirstFailure: firstFailure ?? deadLetterEvent.FailedAt,
            LastFailure: deadLetterEvent.FailedAt
        );
    }

    private static string SerializePayload(object payload)
    {
        return payload is string s ? s : JsonSerializer.Serialize(payload);
    }
}
