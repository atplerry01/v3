namespace Whycespace.Engines.T1M.WSS.Timeout;

/// <summary>
/// Immutable result of a workflow timeout evaluation.
/// Produced by the WorkflowTimeoutEngine after comparing elapsed execution time
/// against the configured timeout threshold.
/// </summary>
public sealed record WorkflowTimeoutResult(
    string WorkflowInstanceId,
    string StepId,
    bool TimedOut,
    TimeSpan ElapsedTime,
    TimeSpan TimeoutThreshold,
    DateTimeOffset EvaluatedAt
)
{
    public static WorkflowTimeoutResult Ok(
        string workflowInstanceId,
        string stepId,
        bool timedOut,
        TimeSpan elapsedTime,
        TimeSpan timeoutThreshold,
        DateTimeOffset evaluatedAt)
        => new(workflowInstanceId, stepId, timedOut, elapsedTime, timeoutThreshold, evaluatedAt);

    public static WorkflowTimeoutResult Fail(string workflowInstanceId, string stepId, DateTimeOffset evaluatedAt)
        => new(workflowInstanceId, stepId, false, TimeSpan.Zero, TimeSpan.Zero, evaluatedAt);
}
