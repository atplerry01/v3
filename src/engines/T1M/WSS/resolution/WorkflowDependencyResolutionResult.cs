namespace Whycespace.Engines.T1M.WSS.Resolution;

/// <summary>
/// Result of workflow step dependency resolution.
/// Categorizes steps as ready (all dependencies met), blocked (waiting), or completed.
/// </summary>
public sealed record WorkflowDependencyResolutionResult(
    string WorkflowId,
    IReadOnlyList<string> ReadySteps,
    IReadOnlyList<string> BlockedSteps,
    IReadOnlyList<string> CompletedSteps,
    DateTimeOffset EvaluatedAt
);
