namespace Whycespace.EngineRuntime.Tests;

using Whycespace.Contracts.Engines;
using Whycespace.EngineRuntime.Registry;
using Whycespace.EngineRuntime.Resolver;

public class EngineResolverTests
{
    [Fact]
    public void Resolve_RegisteredEngine_ReturnsEngine()
    {
        var registry = new EngineRegistry();
        var engine = new StubEngine("MyEngine");
        registry.Register(engine);

        var resolver = new EngineResolver(registry);
        var resolved = resolver.Resolve("MyEngine");

        Assert.Same(engine, resolved);
    }

    [Fact]
    public void Resolve_UnregisteredEngine_Throws()
    {
        var registry = new EngineRegistry();
        var resolver = new EngineResolver(registry);

        Assert.Throws<InvalidOperationException>(
            () => resolver.Resolve("Missing"));
    }

    private sealed class StubEngine : IEngine
    {
        public string Name { get; }

        public StubEngine(string name) => Name = name;

        public Task<EngineResult> ExecuteAsync(EngineContext context)
            => Task.FromResult(EngineResult.Ok(Array.Empty<EngineEvent>()));
    }
}
