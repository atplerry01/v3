namespace Whycespace.Runtime.Persistence.Workflow;

using Whycespace.Systems.Midstream.WSS.Registry;

public interface IWorkflowInstanceStore
{
    void Insert(WorkflowInstanceRecord record);

    void UpdateStatus(string instanceId, WorkflowInstanceRecord record);

    WorkflowInstanceRecord? GetById(string instanceId);

    IReadOnlyList<WorkflowInstanceRecord> GetByWorkflowName(string workflowName);

    WorkflowInstanceRecord? GetByCorrelationId(string correlationId);

    IReadOnlyList<WorkflowInstanceRecord> GetActive();
}
