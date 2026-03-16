namespace Whycespace.Systems.Midstream.WSS.Models;

public sealed record LifecycleDecision(
    string InstanceId,
    string? CurrentStep,
    string? NextStep,
    WorkflowInstanceStatus Status,
    string? Reason
);
