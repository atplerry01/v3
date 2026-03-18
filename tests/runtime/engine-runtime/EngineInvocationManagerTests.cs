namespace Whycespace.EngineRuntime.Tests;

using Whycespace.Contracts.Engines;
using Whycespace.Shared.Primitives.Common;
using Whycespace.EngineRuntime.Invocation;

public class EngineInvocationManagerTests
{
    [Fact]
    public async Task InvokeAsync_CallsEngineAndReturnsResult()
    {
        var expectedResult = EngineResult.Ok(
            new[] { EngineEvent.Create("TestEvent", Guid.NewGuid()) },
            new Dictionary<string, object> { ["key"] = "value" });

        var engine = new StubEngine("Invoker", expectedResult);
        var manager = new EngineInvocationManager();

        var context = new EngineContext(
            Guid.NewGuid(), "wf-1", "step-1",
            new PartitionKey("p1"),
            new Dictionary<string, object>());

        var result = await manager.InvokeAsync(engine, context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("value", result.Output["key"]);
    }

    [Fact]
    public async Task InvokeAsync_EngineFailure_ReturnsFailResult()
    {
        var engine = new StubEngine("Failing", EngineResult.Fail("boom"));
        var manager = new EngineInvocationManager();

        var context = new EngineContext(
            Guid.NewGuid(), "wf-1", "step-1",
            new PartitionKey("p1"),
            new Dictionary<string, object>());

        var result = await manager.InvokeAsync(engine, context);

        Assert.False(result.Success);
    }

    private sealed class StubEngine : IEngine
    {
        public string Name { get; }
        private readonly EngineResult _result;

        public StubEngine(string name, EngineResult result)
        {
            Name = name;
            _result = result;
        }

        public Task<EngineResult> ExecuteAsync(EngineContext context)
            => Task.FromResult(_result);
    }
}
