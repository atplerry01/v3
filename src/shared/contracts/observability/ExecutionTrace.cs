namespace Whycespace.Contracts.Observability;

public sealed record ExecutionTrace(
    string TraceId,
    string OperationName,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    bool IsSuccessful,
    IReadOnlyList<ExecutionTraceStep> Steps
);

public sealed record ExecutionTraceStep(
    string StepName,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    bool IsSuccessful,
    string? ErrorMessage = null
);
