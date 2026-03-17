namespace Whycespace.Engines.T1M.WSS.Graph;

using Whycespace.Engines.T1M.WSS.Workflows;

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
