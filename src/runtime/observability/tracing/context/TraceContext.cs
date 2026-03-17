namespace Whycespace.Runtime.Observability.Tracing.Context;

public sealed record TraceContext(
    Guid TraceId,
    Guid SpanId,
    Guid? ParentSpanId,
    DateTime Timestamp
);
