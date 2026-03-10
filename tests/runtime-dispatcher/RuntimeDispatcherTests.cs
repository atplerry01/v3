namespace Whycespace.RuntimeDispatcher.Tests;

using Whycespace.CommandSystem.Idempotency;
using Whycespace.CommandSystem.Models;
using Whycespace.CommandSystem.Validation;
using Whycespace.Contracts.Runtime;
using Whycespace.Contracts.Workflows;
using Whycespace.RuntimeDispatcher.Resolver;
using Whycespace.WorkflowRuntime.Executor;
using Whycespace.WorkflowRuntime.Registry;
using RtDispatcher = Whycespace.RuntimeDispatcher.Dispatcher.RuntimeDispatcher;
using WfRuntime = Whycespace.WorkflowRuntime.Runtime.WorkflowRuntime;

public class RuntimeDispatcherTests
{
    private readonly CommandValidator _validator = new();
    private readonly InMemoryIdempotencyRegistry _idempotency = new();
    private readonly WorkflowResolver _resolver = new();
    private readonly RtDispatcher _dispatcher;

    public RuntimeDispatcherTests()
    {
        var registry = new WorkflowRegistry();
        registry.Register(new WorkflowGraph(
            "wf-ride", "RideRequestWorkflow",
            new[] { new WorkflowStep("step-1", "Start", "RideExecutionEngine", Array.Empty<string>()) }));

        var executor = new WorkflowExecutor((step, wfId, pk, ctx) =>
            Task.FromResult(Whycespace.Contracts.Engines.EngineResult.Ok(
                new List<Whycespace.Contracts.Engines.EngineEvent>(),
                new Dictionary<string, object>())));

        var runtime = new WfRuntime(registry, executor);
        _dispatcher = new RtDispatcher(_validator, _idempotency, _resolver, runtime);
    }

    [Fact]
    public async Task DispatchAsync_ValidCommand_ExecutesWorkflow()
    {
        var envelope = new CommandEnvelope(
            Guid.NewGuid(),
            "RequestRideCommand",
            new Dictionary<string, object> { ["userId"] = "user-1" },
            DateTimeOffset.UtcNow);

        var result = await _dispatcher.DispatchAsync(envelope, CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task DispatchAsync_DuplicateCommand_Throws()
    {
        var id = Guid.NewGuid();
        var envelope = new CommandEnvelope(
            id, "RequestRideCommand",
            new Dictionary<string, object>(),
            DateTimeOffset.UtcNow);

        await _dispatcher.DispatchAsync(envelope, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _dispatcher.DispatchAsync(envelope, CancellationToken.None));
        Assert.Contains("Duplicate", ex.Message);
    }

    [Fact]
    public async Task DispatchAsync_InvalidCommand_Throws()
    {
        var envelope = new CommandEnvelope(
            Guid.Empty,
            "RequestRideCommand",
            new Dictionary<string, object>(),
            DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _dispatcher.DispatchAsync(envelope, CancellationToken.None));
    }

    [Fact]
    public async Task DispatchAsync_UnmappedCommand_Throws()
    {
        var envelope = new CommandEnvelope(
            Guid.NewGuid(),
            "UnknownCommand",
            new Dictionary<string, object>(),
            DateTimeOffset.UtcNow);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _dispatcher.DispatchAsync(envelope, CancellationToken.None));
        Assert.Contains("No workflow mapped", ex.Message);
    }
}
