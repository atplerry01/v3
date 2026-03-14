namespace Whycespace.EventObservability.Metrics.Models;

public sealed record PartitionMetrics
(
    long PartitionsHealthy,
    long PartitionsDegraded,
    long PartitionsCircuitOpen
);
