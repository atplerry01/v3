namespace Whycespace.Reliability.DeadLetter.Models;

public sealed record DeadLetterMetadata(
    Guid EventId,
    int RetryCount,
    DateTime FirstFailure,
    DateTime LastFailure
);
