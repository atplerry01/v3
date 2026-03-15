namespace Whycespace.Domain.Core.Workflows;

public sealed record WorkflowNode(
    string NodeId,
    string StepId,
    string StepName,
    string EngineName
);
