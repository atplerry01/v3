namespace Whycespace.WorkflowRuntime.Dispatcher;

public sealed record WorkflowDispatchResult(
    Guid InvocationId,
    Guid WorkflowInstanceId,
    string EngineName,
    ExecutionStatus ExecutionStatus,
    Guid? EventId,
    string? EvidenceHash,
    DateTimeOffset ExecutedAt
)
{
    public static WorkflowDispatchResult Executed(
        Guid invocationId,
        Guid workflowInstanceId,
        string engineName,
        Guid eventId,
        string evidenceHash)
        => new(invocationId, workflowInstanceId, engineName, ExecutionStatus.Executed, eventId, evidenceHash, DateTimeOffset.UtcNow);

    public static WorkflowDispatchResult BlockedByPolicy(
        Guid invocationId,
        Guid workflowInstanceId,
        string engineName)
        => new(invocationId, workflowInstanceId, engineName, ExecutionStatus.BlockedByPolicy, null, null, DateTimeOffset.UtcNow);

    public static WorkflowDispatchResult Failed(
        Guid invocationId,
        Guid workflowInstanceId,
        string engineName)
        => new(invocationId, workflowInstanceId, engineName, ExecutionStatus.Failed, null, null, DateTimeOffset.UtcNow);
}

public enum ExecutionStatus
{
    Executed,
    BlockedByPolicy,
    Failed
}