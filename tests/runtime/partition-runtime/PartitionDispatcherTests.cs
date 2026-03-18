namespace Whycespace.PartitionRuntime.Tests;

using Whycespace.CommandSystem.Core.Models;
using Whycespace.Contracts.Engines;
using Whycespace.Shared.Primitives.Common;
using Whycespace.Contracts.Runtime;
using Whycespace.Contracts.Workflows;
using Whycespace.PartitionRuntime.Dispatcher;
using Whycespace.PartitionRuntime.Resolver;
using Whycespace.PartitionRuntime.Router;
using Whycespace.PartitionRuntime.Worker;
using Whycespace.WorkflowRuntime.Executor;
using Whycespace.WorkflowRuntime.Registry;
using WfRuntime = Whycespace.WorkflowRuntime.Runtime.WorkflowRuntime;

public class PartitionDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_RoutesToCorrectPartition()
    {
        var registry = new WorkflowRegistry();
        registry.Register(new WorkflowGraph(
            "wf-ride", "RideRequestWorkflow",
            new[] { new WorkflowStep("step-1", "Start", "RideExecutionEngine", Array.Empty<string>()) }));

        var executor = new WorkflowExecutor((step, wfId, pk, ctx) =>
            Task.FromResult(EngineResult.Ok(
                new List<EngineEvent>(),
                new Dictionary<string, object>())));

        var runtime = new WfRuntime(registry, executor);
        var resolver = new PartitionKeyResolver();
        var router = new PartitionRouter(16);
        var pool = new PartitionWorkerPool(16, runtime);
        var dispatcher = new WorkflowPartitionDispatcher(resolver, router, pool);

        var envelope = new CommandEnvelope(
            Guid.NewGuid(),
            "RequestRideCommand",
            new Dictionary<string, object> { ["RiderId"] = "rider-42" },
            DateTimeOffset.UtcNow);

        var request = new WorkflowExecutionRequest(
            WorkflowName: "RideRequestWorkflow",
            Context: envelope.Payload,
            CorrelationId: envelope.CommandId.ToString());

        var result = await dispatcher.DispatchAsync(envelope, request, CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact]
    public void ResolveAssignment_ReturnsDeterministicAssignment()
    {
        var resolver = new PartitionKeyResolver();
        var router = new PartitionRouter(16);
        var pool = new PartitionWorkerPool(16, null!);
        var dispatcher = new WorkflowPartitionDispatcher(resolver, router, pool);

        var envelope = new CommandEnvelope(
            Guid.NewGuid(),
            "RequestRideCommand",
            new Dictionary<string, object> { ["RiderId"] = "rider-42" },
            DateTimeOffset.UtcNow);

        var assignment1 = dispatcher.ResolveAssignment(envelope);
        var assignment2 = dispatcher.ResolveAssignment(envelope);

        Assert.Equal(assignment1.PartitionKey, assignment2.PartitionKey);
        Assert.Equal(assignment1.PartitionId, assignment2.PartitionId);
        Assert.Equal("rider-42", assignment1.PartitionKey.Value);
    }
}
