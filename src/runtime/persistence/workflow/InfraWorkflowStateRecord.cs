namespace Whycespace.Runtime.Persistence.Workflow;

using Whycespace.Engines.T1M.Shared;

public sealed record WorkflowStateRecord(
    string InstanceId,
    string WorkflowId,
    string CurrentStep,
    IReadOnlyList<WorkflowStepState> CompletedSteps,
    IReadOnlyList<WorkflowStepState> FailedSteps,
    int RetryCount,
    bool TimeoutStatus,
    DateTimeOffset LastUpdated
);
