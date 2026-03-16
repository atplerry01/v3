namespace Whycespace.Systems.Midstream.WSS.Stores;

using Whycespace.Domain.Core.Workflows;

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
