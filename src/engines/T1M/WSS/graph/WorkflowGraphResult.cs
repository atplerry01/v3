namespace Whycespace.Engines.T1M.WSS.Graph;

using Whycespace.Domain.Core.Workflows;

public sealed record WorkflowGraphResult(
    bool Success,
    string? ErrorMessage,
    WorkflowExecutionGraph? Graph
)
{
    public static WorkflowGraphResult Ok(WorkflowExecutionGraph graph)
        => new(true, null, graph);

    public static WorkflowGraphResult Fail(string error)
        => new(false, error, null);
}
