namespace Whycespace.Systems.WSS.Registry;

using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Stores;

public sealed class WorkflowInstanceRegistry : IWorkflowInstanceRegistry
{
    private readonly IWorkflowInstanceRegistryStore _store;

    public WorkflowInstanceRegistry(IWorkflowInstanceRegistryStore store)
    {
        _store = store;
    }

    public WorkflowInstance CreateInstance(string workflowId, string version, IDictionary<string, object>? context)
    {
        var instanceId = $"wf-{workflowId}-{DateTimeOffset.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}";

        var instance = new WorkflowInstance(
            instanceId,
            workflowId,
            version,
            "",
            WorkflowInstanceStatus.Created,
            DateTimeOffset.UtcNow,
            null,
            context != null ? new Dictionary<string, object>(context) : new Dictionary<string, object>());

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

        var updated = existing with
        {
            CurrentStep = currentStep,
            Status = status,
            CompletedAt = status is WorkflowInstanceStatus.Completed or WorkflowInstanceStatus.Failed or WorkflowInstanceStatus.Cancelled
                ? DateTimeOffset.UtcNow
                : existing.CompletedAt
        };

        _store.Update(updated);
        return updated;
    }

    public void RemoveInstance(string instanceId)
    {
        _store.Remove(instanceId);
    }
}
