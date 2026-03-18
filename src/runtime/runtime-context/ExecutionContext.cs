namespace Whycespace.Runtime.Context;

public sealed record ExecutionContext(
    Guid ExecutionId,
    DateTimeOffset StartedAt,
    string? WorkflowInstanceId = null,
    string? StepId = null,
    string? EngineId = null
);
