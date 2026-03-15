namespace Whycespace.Domain.Core.Workflows;

public sealed record WorkflowEdge(
    string FromNode,
    string ToNode
);
