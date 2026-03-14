namespace Whycespace.Reliability.DeadLetter.Models;

public enum DeadLetterReason
{
    RetryLimitExceeded,
    InvalidPayload,
    SchemaViolation,
    PolicyViolation,
    EngineFailure,
    InfrastructureFailure
}
