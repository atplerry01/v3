namespace Whycespace.PartitionRuntime.Tests;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Primitives;
using Whycespace.Contracts.Runtime;
using Whycespace.Contracts.Workflows;
using Whycespace.PartitionRuntime.Worker;
using Whycespace.WorkflowRuntime.Executor;
using Whycespace.WorkflowRuntime.Registry;
using WfRuntime = Whycespace.WorkflowRuntime.Runtime.WorkflowRuntime;

public class PartitionWorkerTests
{
    [Fact]
    public async Task EnqueueAsync_ExecutesWorkflowSuccessfully()
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
        var worker = new PartitionWorker(3, runtime);

        var request = new WorkflowExecutionRequest(
            WorkflowName: "RideRequestWorkflow",
            Context: new Dictionary<string, object> { ["RiderId"] = "rider-42" },
            CorrelationId: Guid.NewGuid().ToString(),
            PartitionKey: new PartitionKey("rider-42"));

        var result = await worker.EnqueueAsync(request);

        Assert.True(result.Success);
        Assert.Equal(3, worker.PartitionId);
    }

    [Fact]
    public void PartitionWorkerPool_GetWorker_ReturnsCorrectWorker()
    {
        var registry = new WorkflowRegistry();
        var executor = new WorkflowExecutor((step, wfId, pk, ctx) =>
            Task.FromResult(EngineResult.Ok(
                new List<EngineEvent>(),
                new Dictionary<string, object>())));

        var runtime = new WfRuntime(registry, executor);
        var pool = new PartitionWorkerPool(16, runtime);

        var worker = pool.GetWorker(5);

        Assert.Equal(5, worker.PartitionId);
        Assert.Equal(16, pool.PartitionCount);
    }

    [Fact]
    public void PartitionWorkerPool_GetActivePartitions_ReturnsAll()
    {
        var registry = new WorkflowRegistry();
        var executor = new WorkflowExecutor((step, wfId, pk, ctx) =>
            Task.FromResult(EngineResult.Ok(
                new List<EngineEvent>(),
                new Dictionary<string, object>())));

        var runtime = new WfRuntime(registry, executor);
        var pool = new PartitionWorkerPool(4, runtime);

        var partitions = pool.GetActivePartitions();

        Assert.Equal(new[] { 0, 1, 2, 3 }, partitions);
    }
}
