namespace Whycespace.WorkflowRuntime;

public sealed record WorkflowRetryPolicyResult(
    string WorkflowInstanceId,
    string StepId,
    bool RetryAllowed,
    TimeSpan RetryDelay,
    int RetryCount,
    DateTimeOffset DecisionTimestamp);
