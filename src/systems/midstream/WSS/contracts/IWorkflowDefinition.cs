namespace Whycespace.Systems.Midstream.WSS.Contracts;

using Whycespace.Contracts.Workflows;

public interface IWorkflowDefinition
{
    string WorkflowName { get; }
    WorkflowGraph BuildGraph();
}
