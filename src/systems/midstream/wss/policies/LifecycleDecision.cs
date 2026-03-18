namespace Whycespace.Systems.Midstream.WSS.Policies;

using Whycespace.Systems.Midstream.WSS.Execution;

public sealed record LifecycleDecision(
    string InstanceId,
    string? CurrentStep,
    string? NextStep,
    WorkflowInstanceStatus Status,
    string? Reason
);
