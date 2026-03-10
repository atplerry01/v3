namespace Whycespace.Shared.Workflow;

public sealed record WorkflowGraph(
    string WorkflowId,
    string Name,
    IReadOnlyList<WorkflowStep> Steps
);

public sealed record WorkflowStep(
    string StepId,
    string Name,
    string EngineName,
    IReadOnlyList<string> NextSteps
);
