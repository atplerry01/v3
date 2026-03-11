namespace Whycespace.Contracts.Workflows;

public sealed record WorkflowStepMapping(
    string StepId,
    string EngineName,
    string CommandName
);
