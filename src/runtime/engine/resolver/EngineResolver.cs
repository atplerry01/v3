namespace Whycespace.EngineRuntime.Resolver;

using Whycespace.Contracts.Engines;
using Whycespace.EngineRuntime.Registry;

public sealed class EngineResolver
{
    private readonly IEngineRegistry _registry;

    public EngineResolver(IEngineRegistry registry)
    {
        _registry = registry;
    }

    public IEngine Resolve(string engineName)
    {
        return _registry.Resolve(engineName)
            ?? throw new InvalidOperationException(
                $"Engine '{engineName}' not registered");
    }
}
