namespace Whycespace.Engines.T1M.WSS.Runtime;

/// <summary>
/// Result of retry policy evaluation.
/// Indicates whether a retry is allowed and the calculated delay.
/// </summary>
public sealed record WorkflowRetryPolicyResult(
    string WorkflowInstanceId,
    string StepId,
    bool RetryAllowed,
    TimeSpan RetryDelay,
    int RetryCount,
    DateTimeOffset DecisionTimestamp
);
