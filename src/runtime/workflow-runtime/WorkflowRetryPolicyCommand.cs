namespace Whycespace.WorkflowRuntime;

using Whycespace.Engines.T1M.WSS.Workflows;

public sealed record WorkflowRetryPolicyCommand(
    string WorkflowInstanceId,
    string StepId,
    RetryPolicy RetryPolicy,
    int CurrentRetryCount,
    DateTimeOffset LastFailureTimestamp);
