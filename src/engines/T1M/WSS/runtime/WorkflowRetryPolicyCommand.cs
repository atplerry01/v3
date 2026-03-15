namespace Whycespace.Engines.T1M.WSS.Runtime;

using Whycespace.Domain.Core.Workflows;

/// <summary>
/// Input command for the Workflow Retry Policy Engine.
/// Contains retry policy configuration and current failure state for evaluation.
/// </summary>
public sealed record WorkflowRetryPolicyCommand(
    string WorkflowInstanceId,
    string StepId,
    RetryPolicy RetryPolicy,
    int CurrentRetryCount,
    DateTimeOffset LastFailureTimestamp
)
{
    public static WorkflowRetryPolicyCommand? FromContextData(IReadOnlyDictionary<string, object> data)
    {
        var instanceId = data.GetValueOrDefault("workflowInstanceId") as string;
        var stepId = data.GetValueOrDefault("stepId") as string;

        if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(stepId))
            return null;

        var retryPolicy = ResolveRetryPolicy(data);
        if (retryPolicy is null)
            return null;

        var currentRetryCount = data.GetValueOrDefault("currentRetryCount") is int count ? count : 0;

        var lastFailure = data.TryGetValue("lastFailureTimestamp", out var ts) && ts is DateTimeOffset dto
            ? dto
            : DateTimeOffset.UtcNow;

        return new WorkflowRetryPolicyCommand(instanceId, stepId, retryPolicy, currentRetryCount, lastFailure);
    }

    private static RetryPolicy? ResolveRetryPolicy(IReadOnlyDictionary<string, object> data)
    {
        if (data.GetValueOrDefault("retryPolicy") is RetryPolicy policy)
            return policy;

        var maxRetries = data.GetValueOrDefault("maxRetries") is int mr ? mr : -1;
        if (maxRetries < 0)
            return null;

        var strategyStr = data.GetValueOrDefault("retryStrategy") as string ?? "FixedDelay";
        if (!Enum.TryParse<RetryStrategy>(strategyStr, true, out var strategy))
            strategy = RetryStrategy.FixedDelay;

        var initialDelayMs = data.GetValueOrDefault("initialDelayMs") is int ms ? ms
            : data.GetValueOrDefault("initialDelayMs") is double dms ? (int)dms
            : 1000;

        var backoffMultiplier = data.GetValueOrDefault("backoffMultiplier") is double bm ? bm
            : data.GetValueOrDefault("backoffMultiplier") is int bmi ? (double)bmi
            : 2.0;

        return new RetryPolicy(maxRetries, strategy, TimeSpan.FromMilliseconds(initialDelayMs), backoffMultiplier);
    }
}
