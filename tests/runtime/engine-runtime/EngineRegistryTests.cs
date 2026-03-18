namespace Whycespace.EngineRuntime.Tests;

using Whycespace.Contracts.Engines;
using Whycespace.EngineRuntime.Registry;

public class EngineRegistryTests
{
    private readonly EngineRegistry _registry = new();

    [Fact]
    public void Register_AddsEngine()
    {
        var engine = new StubEngine("TestEngine");
        _registry.Register(engine);

        var resolved = _registry.Resolve("TestEngine");
        Assert.Same(engine, resolved);
    }

    [Fact]
    public void Resolve_UnregisteredEngine_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => _registry.Resolve("NonExistent"));
        Assert.Contains("NonExistent", ex.Message);
    }

    [Fact]
    public void ListEngines_ReturnsRegisteredNames()
    {
        _registry.Register(new StubEngine("Alpha"));
        _registry.Register(new StubEngine("Beta"));

        var names = _registry.ListEngines();
        Assert.Contains("Alpha", names);
        Assert.Contains("Beta", names);
        Assert.Equal(2, names.Count);
    }

    [Fact]
    public void Register_OverwritesSameNameEngine()
    {
        var first = new StubEngine("Same");
        var second = new StubEngine("Same");

        _registry.Register(first);
        _registry.Register(second);

        var resolved = _registry.Resolve("Same");
        Assert.Same(second, resolved);
        Assert.Single(_registry.ListEngines());
    }

    private sealed class StubEngine : IEngine
    {
        public string Name { get; }

        public StubEngine(string name) => Name = name;

        public Task<EngineResult> ExecuteAsync(EngineContext context)
            => Task.FromResult(EngineResult.Ok(Array.Empty<EngineEvent>()));
    }
}
