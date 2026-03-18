namespace Whycespace.Shared.Protocols.Idempotency;

public sealed record IdempotencyPolicy(
    IdempotencyScope Scope,
    TimeSpan RetentionPeriod,
    bool EnforceOnRetry = true
)
{
    public static IdempotencyPolicy Default => new(
        IdempotencyScope.PerPartition,
        TimeSpan.FromHours(24)
    );
}
