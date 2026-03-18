namespace Whycespace.WorkerPoolRuntime.Tests;

using Whycespace.WorkerPoolRuntime.Models;
using Whycespace.WorkerPoolRuntime.Queue;

public class EngineExecutionQueueTests
{
    [Fact]
    public void Enqueue_IncreasesCount()
    {
        var queue = new EngineExecutionQueue();
        queue.Enqueue(new EngineExecutionTask("TestEngine", new { }));

        Assert.Equal(1, queue.Count());
    }

    [Fact]
    public void Dequeue_ReturnsEnqueuedTask()
    {
        var queue = new EngineExecutionQueue();
        var task = new EngineExecutionTask("TestEngine", "input");

        queue.Enqueue(task);
        var result = queue.Dequeue();

        Assert.NotNull(result);
        Assert.Equal("TestEngine", result!.EngineName);
        Assert.Equal("input", result.Input);
    }

    [Fact]
    public void Dequeue_EmptyQueue_ReturnsNull()
    {
        var queue = new EngineExecutionQueue();
        var result = queue.Dequeue();

        Assert.Null(result);
    }

    [Fact]
    public void Dequeue_DecreasesCount()
    {
        var queue = new EngineExecutionQueue();
        queue.Enqueue(new EngineExecutionTask("A", 1));
        queue.Enqueue(new EngineExecutionTask("B", 2));

        queue.Dequeue();

        Assert.Equal(1, queue.Count());
    }

    [Fact]
    public void Queue_MaintainsFIFOOrder()
    {
        var queue = new EngineExecutionQueue();
        queue.Enqueue(new EngineExecutionTask("First", 1));
        queue.Enqueue(new EngineExecutionTask("Second", 2));
        queue.Enqueue(new EngineExecutionTask("Third", 3));

        Assert.Equal("First", queue.Dequeue()!.EngineName);
        Assert.Equal("Second", queue.Dequeue()!.EngineName);
        Assert.Equal("Third", queue.Dequeue()!.EngineName);
    }

    [Fact]
    public void Count_EmptyQueue_ReturnsZero()
    {
        var queue = new EngineExecutionQueue();
        Assert.Equal(0, queue.Count());
    }
}
