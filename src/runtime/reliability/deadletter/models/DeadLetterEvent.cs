namespace Whycespace.Reliability.DeadLetter.Models;

public sealed record DeadLetterEvent(
    Guid EventId,
    string EventType,
    string SourceTopic,
    int Partition,
    long Offset,
    DeadLetterReason Reason,
    string ErrorMessage,
    int RetryCount,
    DateTime FailedAt,
    string Payload
);
