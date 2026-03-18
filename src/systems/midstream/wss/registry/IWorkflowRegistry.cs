namespace Whycespace.Systems.Midstream.WSS.Registry;

using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

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
