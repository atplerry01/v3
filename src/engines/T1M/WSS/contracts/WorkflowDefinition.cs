namespace Whycespace.Engines.T1M.WSS.Workflows;

/// <summary>
/// Domain-level workflow definition representing a complete workflow
/// with its steps, parameters, and metadata.
/// </summary>
public sealed record WorkflowDefinition(
    string WorkflowId,
    string Name,
    string Description,
    string Version,
    IReadOnlyList<WorkflowStepDefinition> Steps,
    IReadOnlyList<WorkflowParameterDefinition> Parameters,
    DateTimeOffset CreatedAt
);

public sealed record WorkflowStepDefinition(
    string StepId,
    string Name,
    string EngineName,
    IReadOnlyList<string> Dependencies,
    WorkflowRetryPolicy? RetryPolicy,
    WorkflowTimeout? Timeout
);

public sealed record WorkflowRetryPolicy(
    int MaxRetries,
    TimeSpan RetryDelay,
    string? CompensationStepId
)
{
    public const int MaxAllowedRetries = 10;
    public static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(30);
}

public sealed record WorkflowTimeout(
    TimeSpan Duration
)
{
    public static readonly TimeSpan MinTimeout = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan MaxTimeout = TimeSpan.FromHours(24);
}

public sealed record WorkflowParameterDefinition(
    string Name,
    string Type,
    bool Required,
    string? DefaultValue
);
