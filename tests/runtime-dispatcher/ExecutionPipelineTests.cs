namespace Whycespace.RuntimeDispatcher.Tests;

using Whycespace.CommandSystem.Idempotency;
using Whycespace.CommandSystem.Models;
using Whycespace.CommandSystem.Validation;
using Whycespace.Contracts.Runtime;
using Whycespace.Contracts.Workflows;
using Whycespace.RuntimeDispatcher.Pipeline;
using Whycespace.RuntimeDispatcher.Resolver;
using Whycespace.WorkflowRuntime.Executor;
using Whycespace.WorkflowRuntime.Registry;
using WfRuntime = Whycespace.WorkflowRuntime.Runtime.WorkflowRuntime;

public class ExecutionPipelineTests
{
    private readonly CommandValidator _validator = new();
    private readonly InMemoryIdempotencyRegistry _idempotency = new();
    private readonly WorkflowResolver _resolver = new();
    private readonly ExecutionPipeline _pipeline;

    public ExecutionPipelineTests()
    {
        var registry = new WorkflowRegistry();
        registry.Register(new WorkflowGraph(
            "wf-ride", "RideRequestWorkflow",
            new[] { new WorkflowStep("step-1", "Start", "RideExecutionEngine", Array.Empty<string>()) }));
        registry.Register(new WorkflowGraph(
            "wf-property", "PropertyListingWorkflow",
            new[] { new WorkflowStep("step-1", "Start", "PropertyExecutionEngine", Array.Empty<string>()) }));

        var executor = new WorkflowExecutor((step, wfId, pk, ctx) =>
            Task.FromResult(Whycespace.Contracts.Engines.EngineResult.Ok(
                new List<Whycespace.Contracts.Engines.EngineEvent>(),
                new Dictionary<string, object>())));

        var runtime = new WfRuntime(registry, executor);
        _pipeline = new ExecutionPipeline(_validator, _idempotency, _resolver, runtime);
    }

    [Fact]
    public async Task ExecuteAsync_ValidCommand_ReturnsSuccess()
    {
        var envelope = new CommandEnvelope(
            Guid.NewGuid(),
            "RequestRideCommand",
            new Dictionary<string, object> { ["userId"] = "user-1" },
            DateTimeOffset.UtcNow);

        var result = await _pipeline.ExecuteAsync(envelope, CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_PropertyCommand_ReturnsSuccess()
    {
        var envelope = new CommandEnvelope(
            Guid.NewGuid(),
            "ListPropertyCommand",
            new Dictionary<string, object> { ["title"] = "Apt 1" },
            DateTimeOffset.UtcNow);

        var result = await _pipeline.ExecuteAsync(envelope, CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_DuplicateCommand_EnforcesIdempotency()
    {
        var envelope = new CommandEnvelope(
            Guid.NewGuid(),
            "RequestRideCommand",
            new Dictionary<string, object>(),
            DateTimeOffset.UtcNow);

        await _pipeline.ExecuteAsync(envelope, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _pipeline.ExecuteAsync(envelope, CancellationToken.None));
        Assert.Contains("Duplicate", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidCommand_ThrowsValidation()
    {
        var envelope = new CommandEnvelope(
            Guid.Empty,
            "RequestRideCommand",
            new Dictionary<string, object>(),
            DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _pipeline.ExecuteAsync(envelope, CancellationToken.None));
    }
}
