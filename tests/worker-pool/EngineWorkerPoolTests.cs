namespace Whycespace.WorkerPoolRuntime.Tests;

using Whycespace.WorkerPoolRuntime.Models;
using Whycespace.WorkerPoolRuntime.Pool;
using Whycespace.WorkerPoolRuntime.Queue;
using Whycespace.WorkerPoolRuntime.Workers;

public class EngineWorkerPoolTests
{
    [Fact]
    public void Pool_CreatesCorrectNumberOfWorkers()
    {
        var queue = new EngineExecutionQueue();
        var pool = new EngineWorkerPool(4, queue);

        Assert.Equal(4, pool.Workers().Count);
    }

    [Fact]
    public void Pool_ZeroWorkers_CreatesEmptyPool()
    {
        var queue = new EngineExecutionQueue();
        var pool = new EngineWorkerPool(0, queue);

        Assert.Empty(pool.Workers());
    }

    [Fact]
    public void Worker_FetchTask_ReturnsFromQueue()
    {
        var queue = new EngineExecutionQueue();
        var worker = new EnginePoolWorker(queue);

        queue.Enqueue(new EngineExecutionTask("Engine1", "data"));

        var task = worker.FetchTask();
        Assert.NotNull(task);
        Assert.Equal("Engine1", task!.EngineName);
    }

    [Fact]
    public void Worker_FetchTask_EmptyQueue_ReturnsNull()
    {
        var queue = new EngineExecutionQueue();
        var worker = new EnginePoolWorker(queue);

        Assert.Null(worker.FetchTask());
    }

    [Fact]
    public void PoolManager_GetWorkers_ReturnsPoolWorkers()
    {
        var queue = new EngineExecutionQueue();
        var pool = new EngineWorkerPool(3, queue);
        var manager = new EngineWorkerPoolManager(pool);

        Assert.Equal(3, manager.GetWorkers().Count);
    }
}
