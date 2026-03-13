namespace Whycespace.EngineRuntime.Registry;

using Whycespace.Contracts.Engines;

public interface IEngineRegistry
{
    void Register(IEngine engine);

    IEngine Resolve(string engineName);

    IReadOnlyCollection<string> ListEngines();
}
