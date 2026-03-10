namespace Whycespace.EngineRuntime.Registry;

using Whycespace.Contracts.Engines;

public sealed class EngineBootstrapper
{
    private readonly IEngineRegistry _registry;

    public EngineBootstrapper(IEngineRegistry registry)
    {
        _registry = registry;
    }

    public void Register(IEngine engine)
    {
        _registry.Register(engine);
    }

    public void RegisterAll(IEnumerable<IEngine> engines)
    {
        foreach (var engine in engines)
            _registry.Register(engine);
    }
}
