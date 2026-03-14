namespace Whycespace.EventObservability.Metrics.Models;

public sealed record EventMetrics
(
    long EventsProcessed,
    long EventsSucceeded,
    long EventsFailed
);
