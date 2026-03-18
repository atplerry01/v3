namespace Whycespace.Runtime.Context;

public sealed record CorrelationContext(
    string CorrelationId,
    string? CausationId,
    string TraceId
);
