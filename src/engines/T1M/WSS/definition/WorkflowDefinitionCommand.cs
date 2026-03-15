namespace Whycespace.Engines.T1M.WSS.Definition;

using Whycespace.Domain.Core.Workflows;

public sealed record WorkflowDefinitionCommand(
    string WorkflowName,
    string WorkflowDescription,
    string WorkflowVersion,
    IReadOnlyList<WorkflowStepInput> WorkflowSteps,
    IReadOnlyList<WorkflowParameterInput> WorkflowParameters,
    string RequestedBy,
    DateTimeOffset Timestamp
)
{
    public static WorkflowDefinitionCommand FromContextData(IReadOnlyDictionary<string, object> data)
    {
        var workflowName = data.GetValueOrDefault("workflowName") as string ?? string.Empty;
        var workflowDescription = data.GetValueOrDefault("workflowDescription") as string ?? string.Empty;
        var workflowVersion = data.GetValueOrDefault("workflowVersion") as string ?? string.Empty;
        var requestedBy = data.GetValueOrDefault("requestedBy") as string ?? string.Empty;

        var timestamp = data.TryGetValue("timestamp", out var ts) && ts is DateTimeOffset dto
            ? dto
            : DateTimeOffset.UtcNow;

        var steps = ResolveSteps(data.GetValueOrDefault("workflowSteps"));
        var parameters = ResolveParameters(data.GetValueOrDefault("workflowParameters"));

        return new WorkflowDefinitionCommand(
            workflowName, workflowDescription, workflowVersion,
            steps, parameters, requestedBy, timestamp);
    }

    private static IReadOnlyList<WorkflowStepInput> ResolveSteps(object? value)
    {
        if (value is IReadOnlyList<WorkflowStepInput> steps)
            return steps;

        if (value is IEnumerable<object> items)
        {
            return items.OfType<IReadOnlyDictionary<string, object>>()
                .Select(d => new WorkflowStepInput(
                    d.GetValueOrDefault("stepId") as string ?? string.Empty,
                    d.GetValueOrDefault("stepName") as string ?? string.Empty,
                    d.GetValueOrDefault("engineName") as string ?? string.Empty,
                    ResolveDependencies(d.GetValueOrDefault("dependencies")),
                    ResolveTimeSpan(d.GetValueOrDefault("timeout")),
                    ResolveRetryPolicy(d.GetValueOrDefault("retryPolicy"))))
                .ToList();
        }

        return Array.Empty<WorkflowStepInput>();
    }

    private static IReadOnlyList<string> ResolveDependencies(object? value)
    {
        if (value is IReadOnlyList<string> list) return list;
        if (value is IEnumerable<object> items) return items.OfType<string>().ToList();
        return Array.Empty<string>();
    }

    private static TimeSpan ResolveTimeSpan(object? value)
    {
        if (value is TimeSpan ts) return ts;
        if (value is double seconds) return TimeSpan.FromSeconds(seconds);
        if (value is int secs) return TimeSpan.FromSeconds(secs);
        return TimeSpan.FromMinutes(5); // default timeout
    }

    private static WorkflowRetryPolicyInput? ResolveRetryPolicy(object? value)
    {
        if (value is WorkflowRetryPolicyInput policy) return policy;
        if (value is IReadOnlyDictionary<string, object> d)
        {
            var maxRetries = d.GetValueOrDefault("maxRetries") is int mr ? mr : 0;
            var delay = ResolveTimeSpan(d.GetValueOrDefault("retryDelay"));
            return new WorkflowRetryPolicyInput(maxRetries, delay);
        }
        return null;
    }

    private static IReadOnlyList<WorkflowParameterInput> ResolveParameters(object? value)
    {
        if (value is IReadOnlyList<WorkflowParameterInput> parameters)
            return parameters;

        if (value is IEnumerable<object> items)
        {
            return items.OfType<IReadOnlyDictionary<string, object>>()
                .Select(d => new WorkflowParameterInput(
                    d.GetValueOrDefault("parameterName") as string ?? string.Empty,
                    d.GetValueOrDefault("parameterType") as string ?? string.Empty,
                    d.GetValueOrDefault("required") is true))
                .ToList();
        }

        return Array.Empty<WorkflowParameterInput>();
    }
}

public sealed record WorkflowStepInput(
    string StepId,
    string StepName,
    string EngineName,
    IReadOnlyList<string> Dependencies,
    TimeSpan Timeout,
    WorkflowRetryPolicyInput? RetryPolicy
);

public sealed record WorkflowRetryPolicyInput(
    int MaxRetries,
    TimeSpan RetryDelay
);

public sealed record WorkflowParameterInput(
    string ParameterName,
    string ParameterType,
    bool Required
);
