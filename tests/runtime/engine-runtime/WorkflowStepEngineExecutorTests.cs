namespace Whycespace.EngineRuntime.Tests;

using Whycespace.Contracts.Engines;
using Whycespace.Shared.Primitives.Common;
using Whycespace.Contracts.Workflows;
using Whycespace.EngineRuntime.Executor;
using Whycespace.EngineRuntime.Invocation;
using Whycespace.EngineRuntime.Registry;
using Whycespace.EngineRuntime.Resolver;

public class WorkflowStepEngineExecutorTests
{
    [Fact]
    public async Task ExecuteStepAsync_ResolvesAndInvokesEngine()
    {
        var expectedResult = EngineResult.Ok(
            new[] { EngineEvent.Create("StepDone", Guid.NewGuid()) },
            new Dictionary<string, object> { ["output"] = "success" });

        var registry = new EngineRegistry();
        registry.Register(new StubEngine("TestEngine", expectedResult));

        var resolver = new EngineResolver(registry);
        var invocation = new EngineInvocationManager();
        var executor = new WorkflowStepEngineExecutor(resolver, invocation);

        var step = new WorkflowStep("step-1", "Step 1", "TestEngine", Array.Empty<string>());
        var context = new Dictionary<string, object> { ["input"] = "data" };

        var result = await executor.ExecuteStepAsync(
            step, "workflow-1", new PartitionKey("pk"), context);

        Assert.True(result.Success);
        Assert.Equal("success", result.Output["output"]);
        Assert.Single(result.Events);
    }

    [Fact]
    public async Task ExecuteStepAsync_UnknownEngine_Throws()
    {
        var registry = new EngineRegistry();
        var resolver = new EngineResolver(registry);
        var invocation = new EngineInvocationManager();
        var executor = new WorkflowStepEngineExecutor(resolver, invocation);

        var step = new WorkflowStep("step-1", "Step 1", "Unknown", Array.Empty<string>());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => executor.ExecuteStepAsync(
                step, "workflow-1", new PartitionKey("pk"),
                new Dictionary<string, object>()));
    }

    [Fact]
    public async Task ExecuteStepAsync_PassesContextToEngine()
    {
        EngineContext? capturedContext = null;
        var registry = new EngineRegistry();
        registry.Register(new CapturingEngine("Capture", ctx =>
        {
            capturedContext = ctx;
        }));

        var resolver = new EngineResolver(registry);
        var invocation = new EngineInvocationManager();
        var executor = new WorkflowStepEngineExecutor(resolver, invocation);

        var step = new WorkflowStep("step-abc", "Step ABC", "Capture", Array.Empty<string>());
        var context = new Dictionary<string, object> { ["key"] = "val" };

        await executor.ExecuteStepAsync(step, "wf-123", new PartitionKey("pk-1"), context);

        Assert.NotNull(capturedContext);
        Assert.Equal("wf-123", capturedContext!.WorkflowId);
        Assert.Equal("step-abc", capturedContext.WorkflowStep);
        Assert.Equal("pk-1", capturedContext.PartitionKey.Value);
        Assert.Equal("val", capturedContext.Data["key"]);
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

    private sealed class CapturingEngine : IEngine
    {
        public string Name { get; }
        private readonly Action<EngineContext> _capture;

        public CapturingEngine(string name, Action<EngineContext> capture)
        {
            Name = name;
            _capture = capture;
        }

        public Task<EngineResult> ExecuteAsync(EngineContext context)
        {
            _capture(context);
            return Task.FromResult(EngineResult.Ok(Array.Empty<EngineEvent>()));
        }
    }
}
