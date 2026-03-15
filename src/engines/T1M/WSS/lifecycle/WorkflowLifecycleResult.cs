namespace Whycespace.Engines.T1M.WSS.Lifecycle;

public sealed record WorkflowLifecycleResult(
    string WorkflowInstanceId,
    WorkflowLifecycleStatus PreviousStatus,
    WorkflowLifecycleStatus NewStatus,
    bool TransitionAccepted,
    string TransitionReason,
    DateTimeOffset EvaluatedAt
)
{
    public static WorkflowLifecycleResult Accepted(
        string instanceId,
        WorkflowLifecycleStatus previousStatus,
        WorkflowLifecycleStatus newStatus,
        string reason,
        DateTimeOffset evaluatedAt)
        => new(instanceId, previousStatus, newStatus, true, reason, evaluatedAt);

    public static WorkflowLifecycleResult Rejected(
        string instanceId,
        WorkflowLifecycleStatus currentStatus,
        string reason,
        DateTimeOffset evaluatedAt)
        => new(instanceId, currentStatus, currentStatus, false, reason, evaluatedAt);
}
