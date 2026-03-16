namespace Whycespace.WorkflowRuntime;

using Whycespace.Domain.Core.Workflows;

public sealed record WorkflowRetryPolicyCommand(
    string WorkflowInstanceId,
    string StepId,
    RetryPolicy RetryPolicy,
    int CurrentRetryCount,
    DateTimeOffset LastFailureTimestamp);
