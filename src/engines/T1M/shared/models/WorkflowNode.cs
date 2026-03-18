namespace Whycespace.Engines.T1M.Shared;

public sealed record WorkflowNode(
    string NodeId,
    string StepId,
    string StepName,
    string EngineName
);
