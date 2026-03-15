namespace Whycespace.EventObservability.Metrics.Models;

public sealed record FailureMetrics
(
    long RetryAttempts,
    long DeadLetterEvents,
    long EngineFailures,
    long InfrastructureFailures
);
