using Whycespace.Engines.T1M.WSS.Definition;
using Whycespace.Engines.T1M.WSS.Registry;
using Whycespace.Runtime.Persistence.Workflow;
using Whycespace.Infrastructure.Persistence.Workflow;
using WfDefinition = Whycespace.Systems.Midstream.WSS.Definition.WorkflowDefinition;
using WfInstance = Whycespace.Systems.Midstream.WSS.Execution.WorkflowInstance;

namespace Whycespace.Tests.WssWorkflows;

internal sealed class VersionStoreAdapter : WorkflowVersioningEngine.IVersionStore
{
    private readonly WorkflowVersionStore _inner = new();
    public void Store(WfDefinition workflow) => _inner.Store(workflow);
    public WfDefinition? Get(string workflowId, string version) => _inner.Get(workflowId, version);
    public WfDefinition? GetLatest(string workflowId) => _inner.GetLatest(workflowId);
    public IReadOnlyList<WfDefinition> GetVersions(string workflowId) => _inner.GetVersions(workflowId);
    public bool VersionExists(string workflowId, string version) => _inner.VersionExists(workflowId, version);
}

internal sealed class InstanceRegistryStoreAdapter : WorkflowInstanceRegistry.IInstanceRegistryStore
{
    private readonly WorkflowInstanceRegistryStore _inner = new();
    public void Save(WfInstance instance) => _inner.Save(instance);
    public WfInstance Get(string instanceId) => _inner.Get(instanceId);
    public IReadOnlyList<WfInstance> GetAll() => _inner.GetAll();
    public void Update(WfInstance instance) => _inner.Update(instance);
    public void Remove(string instanceId) => _inner.Remove(instanceId);
}
