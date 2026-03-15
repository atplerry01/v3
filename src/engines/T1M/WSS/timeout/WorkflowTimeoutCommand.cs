namespace Whycespace.Engines.T1M.WSS.Timeout;

using Whycespace.Domain.Core.Workflows;

/// <summary>
/// Immutable command for evaluating whether a workflow step or instance has exceeded
/// its allowed execution time. Used by the WorkflowTimeoutEngine for stateless,
/// deterministic timeout evaluation.
/// </summary>
public sealed record WorkflowTimeoutCommand(
    string WorkflowInstanceId,
    string StepId,
    DateTimeOffset StepStartedAt,
    DateTimeOffset CurrentTimestamp,
    WorkflowTimeoutPolicy TimeoutPolicy
)
{
    public static WorkflowTimeoutCommand FromContextData(IReadOnlyDictionary<string, object> data)
    {
        var workflowInstanceId = data.GetValueOrDefault("workflowInstanceId") as string ?? string.Empty;
        var stepId = data.GetValueOrDefault("stepId") as string ?? string.Empty;

        var stepStartedAt = data.TryGetValue("stepStartedAt", out var startVal) && startVal is DateTimeOffset startDto
            ? startDto
            : DateTimeOffset.UtcNow;

        var currentTimestamp = data.TryGetValue("currentTimestamp", out var tsVal) && tsVal is DateTimeOffset tsDto
            ? tsDto
            : DateTimeOffset.UtcNow;

        var timeoutPolicy = ResolveTimeoutPolicy(data);

        return new WorkflowTimeoutCommand(workflowInstanceId, stepId, stepStartedAt, currentTimestamp, timeoutPolicy);
    }

    private static WorkflowTimeoutPolicy ResolveTimeoutPolicy(IReadOnlyDictionary<string, object> data)
    {
        if (data.TryGetValue("timeoutPolicy", out var policyObj) && policyObj is WorkflowTimeoutPolicy policy)
            return policy;

        var durationSeconds = data.TryGetValue("timeoutDurationSeconds", out var durObj) && durObj is int durInt
            ? durInt
            : (data.TryGetValue("timeoutDurationSeconds", out var durObj2) && durObj2 is double durDbl
                ? (int)durDbl
                : 30);

        var strategyStr = data.GetValueOrDefault("timeoutStrategy") as string ?? "StepTimeout";
        var strategy = strategyStr.Equals("WorkflowTimeout", StringComparison.OrdinalIgnoreCase)
            ? TimeoutStrategy.WorkflowTimeout
            : TimeoutStrategy.StepTimeout;

        return new WorkflowTimeoutPolicy(TimeSpan.FromSeconds(durationSeconds), strategy);
    }
}
