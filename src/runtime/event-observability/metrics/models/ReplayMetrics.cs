namespace Whycespace.EventObservability.Metrics.Models;

public sealed record ReplayMetrics
(
    long ReplayAttempts,
    long ReplaySucceeded,
    long ReplayRejected
);
