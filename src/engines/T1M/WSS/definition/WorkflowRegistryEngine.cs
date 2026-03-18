namespace Whycespace.Engines.T1M.WSS.Definition;

using Whycespace.Systems.Midstream.WSS.Models;

public sealed class WorkflowRegistryEngine
{
    private readonly IRegistryStore? _registryStore;
    private readonly IDefinitionLookup? _definitionLookup;

    public WorkflowRegistryEngine() { }

    public WorkflowRegistryEngine(IRegistryStore registryStore, IDefinitionLookup definitionLookup)
    {
        _registryStore = registryStore;
        _definitionLookup = definitionLookup;
    }

    public WorkflowRegistryEntry RegisterWorkflow(string workflowId)
    {
        if (_definitionLookup is null)
            throw new InvalidOperationException("Definition lookup is not configured.");

        var definition = _definitionLookup.Get(workflowId);

        var entry = new WorkflowRegistryEntry(
            definition.WorkflowId,
            definition.Name,
            definition.Version,
            WorkflowRegistryStatus.Active,
            DateTimeOffset.UtcNow);

        _registryStore?.Register(entry);
        return entry;
    }

    public WorkflowRegistryEntry GetWorkflow(string workflowId)
    {
        if (_registryStore is null)
            throw new InvalidOperationException("Registry store is not configured.");
        return _registryStore.Get(workflowId);
    }

    public IReadOnlyCollection<WorkflowRegistryEntry> ListWorkflows()
    {
        return _registryStore?.GetAll() ?? Array.Empty<WorkflowRegistryEntry>();
    }

    /// <summary>
    /// Abstraction for registry storage while the persistence layer is migrated.
    /// </summary>
    public interface IRegistryStore
    {
        void Register(WorkflowRegistryEntry entry);
        WorkflowRegistryEntry Get(string workflowId);
        IReadOnlyCollection<WorkflowRegistryEntry> GetAll();
    }

    /// <summary>
    /// Abstraction for definition lookup while the persistence layer is migrated.
    /// </summary>
    public interface IDefinitionLookup
    {
        WorkflowDefinition Get(string workflowId);
    }
}
