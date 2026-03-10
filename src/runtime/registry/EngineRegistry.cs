namespace Whycespace.Runtime.Registry;

using Whycespace.Contracts.Engines;

public sealed class EngineRegistry
{
    private readonly Dictionary<string, IEngine> _engines = new();

    public void Register(IEngine engine)
    {
        _engines[engine.Name] = engine;
    }

    public IEngine? Resolve(string engineName)
    {
        _engines.TryGetValue(engineName, out var engine);
        return engine;
    }

    public IReadOnlyList<string> GetRegisteredEngines() => _engines.Keys.ToList();
}
