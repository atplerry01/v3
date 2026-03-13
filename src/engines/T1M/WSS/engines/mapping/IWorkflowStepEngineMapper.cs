namespace Whycespace.Engines.T1M.WSS.Mapping;

public interface IWorkflowStepEngineMapper
{
    void RegisterEngine(string engineName, string runtimeIdentifier);
    string ResolveEngine(string engineName);
    bool EngineExists(string engineName);
    IReadOnlyDictionary<string, string> ListEngines();
}
