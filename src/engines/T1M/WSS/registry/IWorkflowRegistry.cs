namespace Whycespace.Engines.T1M.WSS.Registry;

using Whycespace.Systems.Midstream.WSS.Models;

public interface IWorkflowRegistry
{
    void RegisterWorkflow(WorkflowDefinition workflow);

    WorkflowDefinition GetWorkflow(string workflowId);

    IReadOnlyCollection<WorkflowDefinition> ListWorkflows();

    bool WorkflowExists(string workflowId);

    void RemoveWorkflow(string workflowId);
}
