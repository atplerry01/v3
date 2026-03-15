namespace Whycespace.Engines.T1M.WSS.Resolution;

/// <summary>
/// Input command for the Workflow Dependency Resolution Engine.
/// Contains the workflow step graph and completed steps to resolve readiness.
/// </summary>
public sealed record WorkflowDependencyCommand(
    string WorkflowId,
    IReadOnlyList<DependencyStep> WorkflowSteps,
    IReadOnlyList<string> CompletedSteps
);

/// <summary>
/// Represents a workflow step with its explicit backward-edge dependencies.
/// A step may execute only when all its dependencies are completed.
/// </summary>
public sealed record DependencyStep(
    string StepId,
    string StepName,
    string EngineName,
    IReadOnlyList<string> Dependencies
);
