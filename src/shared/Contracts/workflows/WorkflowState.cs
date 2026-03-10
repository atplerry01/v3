namespace Whycespace.Contracts.Workflows;

public sealed record WorkflowState(
    string WorkflowId,
    string CurrentStepId,
    WorkflowStatus Status,
    IReadOnlyDictionary<string, object> Context,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt
);

public enum WorkflowStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}
