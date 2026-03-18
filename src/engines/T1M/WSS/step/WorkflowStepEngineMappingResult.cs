namespace Whycespace.Engines.T1M.WSS.Step;

public sealed record WorkflowStepEngineMappingResult(
    bool Success,
    string WorkflowId,
    IReadOnlyList<ResolvedStepEngineMapping> StepEngineMappings,
    DateTimeOffset ResolvedAt,
    string? Error = null
)
{
    public static WorkflowStepEngineMappingResult Ok(
        string workflowId,
        IReadOnlyList<ResolvedStepEngineMapping> mappings,
        DateTimeOffset resolvedAt)
        => new(true, workflowId, mappings, resolvedAt);

    public static WorkflowStepEngineMappingResult Fail(string reason)
        => new(false, string.Empty, Array.Empty<ResolvedStepEngineMapping>(), default, reason);
}

public sealed record ResolvedStepEngineMapping(
    string StepId,
    string EngineName
);
