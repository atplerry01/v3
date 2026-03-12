namespace Whycespace.Engines.T1M.WSS.Mapping;

using Whycespace.Engines.T1M.WSS.Stores;

public sealed class WorkflowStepEngineMapper : IWorkflowStepEngineMapper
{
    private readonly WorkflowEngineMappingStore _store;

    public WorkflowStepEngineMapper(WorkflowEngineMappingStore store)
    {
        _store = store;
    }

    public void RegisterEngine(string engineName, string runtimeIdentifier)
    {
        _store.Register(engineName, runtimeIdentifier);
    }

    public string ResolveEngine(string engineName)
    {
        if (!_store.TryGet(engineName, out var runtimeIdentifier))
            throw new EngineMappingException(engineName);

        return runtimeIdentifier;
    }

    public bool EngineExists(string engineName)
    {
        return _store.Exists(engineName);
    }

    public IReadOnlyDictionary<string, string> ListEngines()
    {
        return _store.GetAll();
    }
}
