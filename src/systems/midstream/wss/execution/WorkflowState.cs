namespace Whycespace.Systems.Midstream.WSS.Execution;

public sealed record WorkflowState(
    string InstanceId,
    string WorkflowId,
    string WorkflowVersion,
    string CurrentStep,
    IReadOnlyList<string> CompletedSteps,
    WorkflowInstanceStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyDictionary<string, object> ExecutionContext
);
