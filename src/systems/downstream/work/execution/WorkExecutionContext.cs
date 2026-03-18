namespace Whycespace.Systems.Downstream.Work.Execution;

public sealed record WorkExecutionContext(
    Guid ExecutionId,
    string ClusterId,
    string SubClusterId,
    string WorkerId,
    string TaskType,
    DateTimeOffset StartedAt,
    IReadOnlyDictionary<string, string>? Metadata = null
);
