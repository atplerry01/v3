namespace Whycespace.Contracts.Observability;

public sealed record MetricsMetadata(
    string MetricName,
    string Unit,
    double Value,
    DateTimeOffset RecordedAt,
    IReadOnlyDictionary<string, string>? Tags = null
);
