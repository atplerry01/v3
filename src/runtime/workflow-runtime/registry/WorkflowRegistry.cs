namespace Whycespace.WorkflowRuntime.Registry;

using global::System.Collections.Concurrent;
using Whycespace.Contracts.Workflows;

public sealed class WorkflowRegistry : IWorkflowRegistry
{
    private readonly ConcurrentDictionary<string, WorkflowGraph> _workflows = new();

    public void Register(WorkflowGraph graph)
    {
        _workflows[graph.Name] = graph;
    }

    public WorkflowGraph? Resolve(string workflowName)
    {
        _workflows.TryGetValue(workflowName, out var graph);
        return graph;
    }

    public IReadOnlyCollection<string> GetRegisteredWorkflows()
    {
        return _workflows.Keys.ToList().AsReadOnly();
    }
}
