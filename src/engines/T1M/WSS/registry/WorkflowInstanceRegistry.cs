namespace Whycespace.Engines.T1M.WSS.Registry;

using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

public sealed class WorkflowInstanceRegistry : IWorkflowInstanceRegistry
{
    private readonly IInstanceRegistryStore? _store;

    public WorkflowInstanceRegistry() { }

    public WorkflowInstanceRegistry(IInstanceRegistryStore store)
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

        _store?.Save(instance);
        return instance;
    }

    public WorkflowInstance GetInstance(string instanceId)
    {
        if (_store is null)
            throw new InvalidOperationException("Instance registry store is not configured.");
        return _store.Get(instanceId);
    }

    public IReadOnlyList<WorkflowInstance> ListInstances()
    {
        return _store?.GetAll() ?? Array.Empty<WorkflowInstance>();
    }

    public WorkflowInstance UpdateInstanceState(string instanceId, string currentStep, WorkflowInstanceStatus status)
    {
        if (_store is null)
            throw new InvalidOperationException("Instance registry store is not configured.");

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
        _store?.Remove(instanceId);
    }

    /// <summary>
    /// Abstraction for instance registry storage while the persistence layer is migrated.
    /// </summary>
    public interface IInstanceRegistryStore
    {
        void Save(WorkflowInstance instance);
        WorkflowInstance Get(string instanceId);
        IReadOnlyList<WorkflowInstance> GetAll();
        void Update(WorkflowInstance instance);
        void Remove(string instanceId);
    }
}
