namespace Whycespace.Systems.WSS.Registry;

using Whycespace.Systems.Midstream.WSS.Models;

public interface IWssWorkflowDefinitionRegistry
{
    void RegisterWorkflow(WorkflowDefinition workflow);
    WorkflowDefinition GetWorkflow(string workflowId);
    IReadOnlyCollection<WorkflowDefinition> ListWorkflows();
    bool WorkflowExists(string workflowId);
    void RemoveWorkflow(string workflowId);
}
