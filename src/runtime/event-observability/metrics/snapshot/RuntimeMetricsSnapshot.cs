using Whycespace.EventObservability.Metrics.Models;

namespace Whycespace.EventObservability.Metrics.Snapshot;

public sealed record RuntimeMetricsSnapshot
(
    Models.EventMetrics EventMetrics,
    FailureMetrics FailureMetrics,
    ReplayMetrics ReplayMetrics,
    PartitionMetrics PartitionMetrics
);
