namespace Whycespace.Engines.T1M.WSS.Step;

public interface IWorkflowStepEngineMapper
{
    void RegisterEngine(string engineName, string runtimeIdentifier);
    string ResolveEngine(string engineName);
    bool EngineExists(string engineName);
    IReadOnlyDictionary<string, string> ListEngines();
}
