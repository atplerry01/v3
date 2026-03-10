namespace Whycespace.WorkflowRuntime.Registry;

using Whycespace.Contracts.Workflows;

public interface IWorkflowRegistry
{
    void Register(WorkflowGraph graph);
    WorkflowGraph? Resolve(string workflowName);
    IReadOnlyCollection<string> GetRegisteredWorkflows();
}
