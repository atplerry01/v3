namespace Whycespace.WorkflowRuntime.Dispatcher;

using global::System.Collections.Concurrent;
using Whycespace.Contracts.Engines;

public sealed class EngineInvocationRegistry
{
    private readonly ConcurrentDictionary<string, EngineRegistration> _engines = new();

    public void Register(IEngine engine, string version = "1.0.0")
    {
        _engines[engine.Name] = new EngineRegistration(engine, version);
    }

    public EngineRegistration? Resolve(string engineName)
    {
        _engines.TryGetValue(engineName, out var registration);
        return registration;
    }

    public bool IsRegistered(string engineName) => _engines.ContainsKey(engineName);

    public IReadOnlyCollection<string> ListEngines() => _engines.Keys.ToList().AsReadOnly();
}

public sealed record EngineRegistration(IEngine Engine, string Version);
