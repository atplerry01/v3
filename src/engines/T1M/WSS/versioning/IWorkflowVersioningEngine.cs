namespace Whycespace.Engines.T1M.WSS.Versioning;

using Whycespace.System.Midstream.WSS.Models;

public interface IWorkflowVersioningEngine
{
    WorkflowDefinition RegisterWorkflowVersion(WorkflowDefinition workflow);

    WorkflowDefinition GetWorkflowVersion(string workflowId, string version);

    WorkflowDefinition GetLatestWorkflow(string workflowId);

    IReadOnlyList<WorkflowDefinition> ListWorkflowVersions(string workflowId);

    bool WorkflowVersionExists(string workflowId, string version);
}
