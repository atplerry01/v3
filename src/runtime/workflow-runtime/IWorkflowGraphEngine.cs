namespace Whycespace.WorkflowRuntime;

using Whycespace.Systems.Midstream.WSS.Models;

/// <summary>
/// Contract for workflow graph operations. Implemented by Whycespace.Engines.T1M.WSS.Graph.WorkflowGraphEngine.
/// </summary>
public interface IWorkflowGraphEngine
{
    WorkflowGraph BuildGraph(WorkflowDefinition definition);
    IReadOnlyList<string> ValidateGraph(WorkflowGraph graph);
    IReadOnlyList<string> GetStartNodes(WorkflowGraph graph);
    IReadOnlyList<string> GetNextNodes(WorkflowGraph graph, string currentNode);
}
