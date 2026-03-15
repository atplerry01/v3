namespace Whycespace.System.Midstream.WSS.Instances;

using Whycespace.System.Midstream.WSS.Models;

public interface IWorkflowInstanceRegistry
{
    WorkflowInstanceRecord CreateWorkflowInstance(
        string workflowName,
        string workflowVersion,
        string correlationId,
        string initiatedBy);

    void UpdateWorkflowInstanceStatus(string instanceId, WorkflowInstanceStatus newStatus);

    WorkflowInstanceRecord? ResolveWorkflowInstance(string instanceId);

    IReadOnlyList<WorkflowInstanceRecord> ListActiveWorkflowInstances();

    WorkflowInstanceRecord? ResolveByCorrelationId(string correlationId);

    IReadOnlyList<WorkflowInstanceRecord> ResolveByWorkflowName(string workflowName);
}
