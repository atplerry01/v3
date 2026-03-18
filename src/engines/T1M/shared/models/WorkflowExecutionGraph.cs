namespace Whycespace.Engines.T1M.Shared;

public sealed record WorkflowExecutionGraph(
    string WorkflowId,
    IReadOnlyList<WorkflowNode> Nodes,
    IReadOnlyList<WorkflowEdge> Edges,
    IReadOnlyList<string> ExecutionOrder,
    IReadOnlyList<IReadOnlyList<string>> ParallelGroups,
    DateTimeOffset CreatedAt
);
