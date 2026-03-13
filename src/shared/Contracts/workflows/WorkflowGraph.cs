namespace Whycespace.Contracts.Workflows;

public sealed record WorkflowGraph(
    string WorkflowId,
    string Name,
    IReadOnlyList<WorkflowStep> Steps
);
