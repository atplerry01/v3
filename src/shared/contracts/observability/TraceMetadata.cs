namespace Whycespace.Contracts.Observability;

public sealed record TraceMetadata(
    string TraceId,
    string SpanId,
    string? ParentSpanId,
    string OperationName,
    DateTimeOffset StartedAt
);
