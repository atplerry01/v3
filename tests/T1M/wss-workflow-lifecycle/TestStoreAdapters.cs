using Whycespace.Engines.T1M.WSS.Registry;
using Whycespace.Runtime.Persistence.Workflow;
using Whycespace.Runtime.Persistence.Workflow;
using WfInstance = Whycespace.Systems.Midstream.WSS.Execution.WorkflowInstance;

namespace Whycespace.WSS.WorkflowLifecycle.Tests;

internal sealed class InstanceRegistryStoreAdapter : WorkflowInstanceRegistry.IInstanceRegistryStore
{
    private readonly WorkflowInstanceRegistryStore _inner = new();
    public void Save(WfInstance instance) => _inner.Save(instance);
    public WfInstance Get(string instanceId) => _inner.Get(instanceId);
    public IReadOnlyList<WfInstance> GetAll() => _inner.GetAll();
    public void Update(WfInstance instance) => _inner.Update(instance);
    public void Remove(string instanceId) => _inner.Remove(instanceId);
}
