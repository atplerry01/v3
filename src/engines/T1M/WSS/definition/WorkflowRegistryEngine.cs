namespace Whycespace.Engines.T1M.WSS.Definition;

using Whycespace.System.Midstream.WSS.Models;
using Whycespace.Engines.T1M.WSS.Stores;

public sealed class WorkflowRegistryEngine
{
    private readonly WorkflowRegistryStore _registryStore;
    private readonly WorkflowDefinitionStore _definitionStore;

    public WorkflowRegistryEngine(WorkflowRegistryStore registryStore, WorkflowDefinitionStore definitionStore)
    {
        _registryStore = registryStore;
        _definitionStore = definitionStore;
    }

    public WorkflowRegistryEntry RegisterWorkflow(string workflowId)
    {
        var definition = _definitionStore.Get(workflowId);

        var entry = new WorkflowRegistryEntry(
            definition.WorkflowId,
            definition.Name,
            definition.Version,
            WorkflowRegistryStatus.Active,
            DateTimeOffset.UtcNow);

        _registryStore.Register(entry);
        return entry;
    }

    public WorkflowRegistryEntry GetWorkflow(string workflowId)
    {
        return _registryStore.Get(workflowId);
    }

    public IReadOnlyCollection<WorkflowRegistryEntry> ListWorkflows()
    {
        return _registryStore.GetAll();
    }
}
