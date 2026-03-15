namespace Whycespace.System.Midstream.WSS.Registry;

using Whycespace.System.Midstream.WSS.Models;

public interface IWorkflowRegistry
{
    WorkflowRegistryRecord RegisterWorkflowDefinition(WorkflowDefinition definition, string createdBy);

    WorkflowRegistryRecord RegisterWorkflowTemplate(WorkflowTemplate template, string createdBy);

    WorkflowRegistryRecord RegisterWorkflowGraph(WorkflowGraph graph, string workflowName, string version, string createdBy);

    WorkflowRegistryRecord? ResolveWorkflow(string workflowName, string? version = null);

    WorkflowRegistryRecord? GetWorkflow(string workflowId);

    IReadOnlyList<WorkflowRegistryRecord> ListWorkflows();

    IReadOnlyList<WorkflowRegistryRecord> ListWorkflowsByType(WorkflowType type);

    void UpdateStatus(string workflowId, WorkflowRegistryRecordStatus status);
}
