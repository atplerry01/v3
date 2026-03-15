namespace Whycespace.Engines.T1M.WSS.Validation;

/// <summary>
/// Command model for the Workflow Validation Engine (2.1.6).
/// Carries all data needed to validate a workflow definition.
/// </summary>
public sealed record WorkflowValidationCommand(
    string WorkflowId,
    string WorkflowName,
    string WorkflowVersion,
    IReadOnlyList<WorkflowValidationStep> WorkflowSteps,
    IReadOnlyList<WorkflowValidationParameter> WorkflowParameters,
    WorkflowValidationGraph WorkflowGraph
);

public sealed record WorkflowValidationStep(
    string StepId,
    string StepName,
    string EngineName,
    IReadOnlyList<string> Dependencies,
    WorkflowStepRetryPolicy? RetryPolicy,
    WorkflowStepTimeout? Timeout
);

public sealed record WorkflowStepRetryPolicy(
    int MaxRetries,
    double RetryDelaySeconds,
    string? CompensationStepId
);

public sealed record WorkflowStepTimeout(
    double TimeoutSeconds
);

public sealed record WorkflowValidationParameter(
    string Name,
    string Type,
    bool Required,
    string? DefaultValue
);

public sealed record WorkflowValidationGraph(
    IReadOnlyDictionary<string, IReadOnlyList<string>> Transitions
);
