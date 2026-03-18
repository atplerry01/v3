namespace Whycespace.Contracts.Workflows;

public sealed record WorkflowStep(
    string StepId,
    string Name,
    string EngineName,
    IReadOnlyList<string> NextSteps
);
