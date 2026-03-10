namespace Whycespace.EngineRuntime.Registry;

using System.Collections.Concurrent;
using Whycespace.Contracts.Engines;

public sealed class EngineRegistry : IEngineRegistry
{
    private readonly ConcurrentDictionary<string, IEngine> _engines = new();

    public void Register(IEngine engine)
    {
        _engines[engine.Name] = engine;
    }

    public IEngine Resolve(string engineName)
    {
        if (_engines.TryGetValue(engineName, out var engine))
            return engine;

        throw new InvalidOperationException($"Engine '{engineName}' is not registered");
    }

    public IReadOnlyCollection<string> ListEngines() => _engines.Keys.ToList().AsReadOnly();
}
