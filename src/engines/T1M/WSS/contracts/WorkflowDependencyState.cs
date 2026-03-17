namespace Whycespace.Engines.T1M.WSS.Workflows;

/// <summary>
/// Domain state representing the dependency resolution outcome for a workflow instance.
/// Tracks which steps are ready, blocked, or completed at a given evaluation point.
/// </summary>
public sealed record WorkflowDependencyState(
    string WorkflowId,
    IReadOnlyList<string> ReadySteps,
    IReadOnlyList<string> BlockedSteps,
    IReadOnlyList<string> CompletedSteps,
    DateTimeOffset EvaluatedAt
);
