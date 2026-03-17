namespace Whycespace.Engines.T1M.WSS.Workflows;

public sealed record WorkflowNode(
    string NodeId,
    string StepId,
    string StepName,
    string EngineName
);
