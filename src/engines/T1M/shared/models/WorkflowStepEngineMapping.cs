namespace Whycespace.Engines.T1M.Shared;

public sealed record WorkflowStepEngineMapping(
    string WorkflowId,
    string StepId,
    string EngineName,
    DateTimeOffset ResolvedAt
);
