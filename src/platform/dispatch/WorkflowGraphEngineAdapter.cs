namespace Whycespace.Platform.Dispatch;

using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;
using Whycespace.WorkflowRuntime;

/// <summary>
/// Adapts the WSS WorkflowGraphEngine (Whycespace.Engines.T1M.WSS.Graph.IWorkflowGraphEngine)
/// to the runtime IWorkflowGraphEngine contract (Whycespace.WorkflowRuntime.IWorkflowGraphEngine).
/// This bridges the method signature differences without modifying either interface.
/// </summary>
internal sealed class WorkflowGraphEngineAdapter : IWorkflowGraphEngine
{
    private readonly Whycespace.Engines.T1M.WSS.Graph.WorkflowGraphEngine _inner;

    public WorkflowGraphEngineAdapter(Whycespace.Engines.T1M.WSS.Graph.WorkflowGraphEngine inner)
    {
        _inner = inner;
    }

    public WorkflowGraph BuildGraph(WorkflowDefinition definition)
    {
        var stepDefs = definition.Steps.Select(s =>
            new WorkflowStepDefinition(s.StepId, s.Name, s.EngineName, "", s.NextSteps, null))
            .ToList();
        return _inner.BuildGraph(stepDefs);
    }

    public IReadOnlyList<string> ValidateGraph(WorkflowGraph graph)
        => _inner.ValidateGraph(graph);

    public IReadOnlyList<string> GetStartNodes(WorkflowGraph graph)
        => _inner.GetStartSteps(graph);

    public IReadOnlyList<string> GetNextNodes(WorkflowGraph graph, string currentNode)
        => _inner.GetNextSteps(graph, currentNode);
}
