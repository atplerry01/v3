namespace Whycespace.Engines.T1M.WSS.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Contracts.Workflows;

public sealed class WorkflowInstanceStore
{
    private readonly ConcurrentDictionary<Guid, WorkflowInstanceEntry> _instances = new();

    public void Save(WorkflowInstanceEntry instance)
    {
        _instances[instance.InstanceId] = instance;
    }

    public WorkflowInstanceEntry? Get(Guid instanceId)
    {
        return _instances.TryGetValue(instanceId, out var instance) ? instance : null;
    }

    public IReadOnlyList<WorkflowInstanceEntry> List()
    {
        return _instances.Values.ToList();
    }

    public IReadOnlyList<WorkflowInstanceEntry> ListByWorkflow(string workflowId)
    {
        return _instances.Values.Where(i => i.WorkflowId == workflowId).ToList();
    }
}
