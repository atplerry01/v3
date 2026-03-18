namespace Whycespace.Engines.T1M.WSS.Step;

using Whycespace.Engines.T1M.Shared;

public sealed class WorkflowStepEngineMapper : IWorkflowStepEngineMapper
{
    private readonly IMappingStore? _store;

    public WorkflowStepEngineMapper() { }

    public WorkflowStepEngineMapper(IMappingStore store)
    {
        _store = store;
    }

    public void RegisterEngine(string engineName, string runtimeIdentifier)
    {
        if (_store is null)
            throw new InvalidOperationException("Engine mapping store is not configured.");
        _store.Register(engineName, runtimeIdentifier);
    }

    public string ResolveEngine(string engineName)
    {
        if (_store is null || !_store.TryGet(engineName, out var runtimeIdentifier))
            throw new EngineMappingException(engineName);

        return runtimeIdentifier;
    }

    public bool EngineExists(string engineName)
    {
        return _store?.Exists(engineName) ?? false;
    }

    public IReadOnlyDictionary<string, string> ListEngines()
    {
        return _store?.GetAll() ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Abstraction for engine mapping storage while the persistence layer is migrated.
    /// </summary>
    public interface IMappingStore
    {
        void Register(string engineName, string runtimeIdentifier);
        bool TryGet(string engineName, out string runtimeIdentifier);
        bool Exists(string engineName);
        IReadOnlyDictionary<string, string> GetAll();
    }
}
