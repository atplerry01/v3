namespace Whycespace.Domain.Core.Workflows;

/// <summary>
/// Domain model representing the current execution state of a workflow step,
/// including retry tracking information.
/// </summary>
public sealed record WorkflowStepState(
    string StepId,
    string StepName,
    StepStatus Status,
    int RetryCount,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? LastFailureTimestamp
);

/// <summary>
/// Execution status of a workflow step.
/// </summary>
public enum StepStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Retrying = 4,
    TimedOut = 5
}
