namespace Whycespace.Engines.T1M.WSS.Instance;

using Whycespace.Engines.T1M.WSS.Stores;
using Whycespace.Systems.Midstream.WSS.Models;

public sealed class WorkflowInstanceRegistry : IWorkflowInstanceRegistry
{
    private readonly WorkflowInstanceRegistryStore _store;

    public WorkflowInstanceRegistry(WorkflowInstanceRegistryStore store)
    {
        _store = store;
    }

    public WorkflowInstance CreateInstance(string workflowId, string version, IDictionary<string, object>? context)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var instanceId = $"wf-{workflowId}-{timestamp:yyyyMMdd}-{Guid.NewGuid():N}";

        var instance = new WorkflowInstance(
            instanceId,
            workflowId,
            version,
            string.Empty,
            WorkflowInstanceStatus.Created,
            timestamp,
            null,
            context is not null ? new Dictionary<string, object>(context) : new Dictionary<string, object>()
        );

        _store.Save(instance);
        return instance;
    }

    public WorkflowInstance GetInstance(string instanceId)
    {
        return _store.Get(instanceId);
    }

    public IReadOnlyList<WorkflowInstance> ListInstances()
    {
        return _store.GetAll();
    }

    public WorkflowInstance UpdateInstanceState(string instanceId, string currentStep, WorkflowInstanceStatus status)
    {
        var existing = _store.Get(instanceId);

        var completedAt = status is WorkflowInstanceStatus.Completed or WorkflowInstanceStatus.Failed or WorkflowInstanceStatus.Cancelled
            ? DateTimeOffset.UtcNow
            : existing.CompletedAt;

        var updated = existing with
        {
            CurrentStep = currentStep,
            Status = status,
            CompletedAt = completedAt
        };

        _store.Update(updated);
        return updated;
    }

    public void RemoveInstance(string instanceId)
    {
        _store.Remove(instanceId);
    }
}
