namespace Whycespace.Engines.T1M.WSS.Workflows;

public sealed record WorkflowStepEngineMapping(
    string WorkflowId,
    string StepId,
    string EngineName,
    DateTimeOffset ResolvedAt
);
