namespace Whycespace.System.Midstream.WSS.Contracts;

using Whycespace.Shared.Workflow;

public interface IWorkflowDefinition
{
    string WorkflowName { get; }
    WorkflowGraph BuildGraph();
}
