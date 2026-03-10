namespace Whycespace.Observability.Tracing;

public sealed record TraceContext(
    Guid TraceId,
    Guid SpanId,
    Guid? ParentSpanId,
    DateTime Timestamp
);
