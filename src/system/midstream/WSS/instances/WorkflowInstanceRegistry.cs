namespace Whycespace.System.Midstream.WSS.Instances;

using Whycespace.System.Midstream.WSS.Models;
using Whycespace.System.Midstream.WSS.Stores;

public sealed class WorkflowInstanceRegistry : IWorkflowInstanceRegistry
{
    private readonly IWorkflowInstanceStore _store;

    public WorkflowInstanceRegistry(IWorkflowInstanceStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public WorkflowInstanceRecord CreateWorkflowInstance(
        string workflowName,
        string workflowVersion,
        string correlationId,
        string initiatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowName);
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(initiatedBy);

        var instanceId = Guid.NewGuid().ToString();
        var workflowId = $"{workflowName}:{workflowVersion}";

        var record = new WorkflowInstanceRecord(
            InstanceId: instanceId,
            WorkflowId: workflowId,
            WorkflowName: workflowName,
            WorkflowVersion: workflowVersion,
            Status: WorkflowInstanceStatus.Created,
            StartedAt: DateTimeOffset.UtcNow,
            CompletedAt: null,
            CorrelationId: correlationId,
            InitiatedBy: initiatedBy
        );

        _store.Insert(record);
        return record;
    }

    public void UpdateWorkflowInstanceStatus(string instanceId, WorkflowInstanceStatus newStatus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        if (!Enum.IsDefined(newStatus))
            throw new InvalidOperationException($"Invalid workflow instance status: {newStatus}");

        var existing = _store.GetById(instanceId)
            ?? throw new KeyNotFoundException($"Workflow instance not found: {instanceId}");

        var completedAt = newStatus is WorkflowInstanceStatus.Completed
            or WorkflowInstanceStatus.Failed
            or WorkflowInstanceStatus.Terminated
            ? DateTimeOffset.UtcNow
            : existing.CompletedAt;

        _store.UpdateStatus(instanceId, existing with
        {
            Status = newStatus,
            CompletedAt = completedAt
        });
    }

    public WorkflowInstanceRecord? ResolveWorkflowInstance(string instanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        return _store.GetById(instanceId);
    }

    public IReadOnlyList<WorkflowInstanceRecord> ListActiveWorkflowInstances()
    {
        return _store.GetActive();
    }

    public WorkflowInstanceRecord? ResolveByCorrelationId(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        return _store.GetByCorrelationId(correlationId);
    }

    public IReadOnlyList<WorkflowInstanceRecord> ResolveByWorkflowName(string workflowName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowName);
        return _store.GetByWorkflowName(workflowName);
    }
}
