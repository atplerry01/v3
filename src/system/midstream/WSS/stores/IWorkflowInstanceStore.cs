namespace Whycespace.System.Midstream.WSS.Stores;

using Whycespace.System.Midstream.WSS.Instances;

public interface IWorkflowInstanceStore
{
    void Insert(WorkflowInstanceRecord record);

    void UpdateStatus(string instanceId, WorkflowInstanceRecord record);

    WorkflowInstanceRecord? GetById(string instanceId);

    IReadOnlyList<WorkflowInstanceRecord> GetByWorkflowName(string workflowName);

    WorkflowInstanceRecord? GetByCorrelationId(string correlationId);

    IReadOnlyList<WorkflowInstanceRecord> GetActive();
}
