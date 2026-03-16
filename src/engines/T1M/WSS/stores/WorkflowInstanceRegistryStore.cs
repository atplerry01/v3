namespace Whycespace.Engines.T1M.WSS.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Midstream.WSS.Models;

public sealed class WorkflowInstanceRegistryStore
{
    private readonly ConcurrentDictionary<string, WorkflowInstance> _instances = new();

    public void Save(WorkflowInstance instance)
    {
        if (!_instances.TryAdd(instance.InstanceId, instance))
            throw new InvalidOperationException($"Instance already exists: '{instance.InstanceId}'");
    }

    public WorkflowInstance Get(string instanceId)
    {
        if (!_instances.TryGetValue(instanceId, out var instance))
            throw new KeyNotFoundException($"Workflow instance not found: '{instanceId}'");
        return instance;
    }

    public IReadOnlyList<WorkflowInstance> GetAll()
    {
        return _instances.Values.ToList();
    }

    public void Update(WorkflowInstance instance)
    {
        if (!_instances.ContainsKey(instance.InstanceId))
            throw new KeyNotFoundException($"Workflow instance not found: '{instance.InstanceId}'");
        _instances[instance.InstanceId] = instance;
    }

    public void Remove(string instanceId)
    {
        if (!_instances.TryRemove(instanceId, out _))
            throw new KeyNotFoundException($"Workflow instance not found: '{instanceId}'");
    }
}
