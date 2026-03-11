using Whycespace.WorkerPoolRuntime.Queue;
using Whycespace.WorkerPoolRuntime.Models;
using Whycespace.WorkerPoolRuntime.Workers;
using System.Collections.Concurrent;

namespace Whycespace.RuntimeConcurrencyTests;

public sealed class WorkerPoolConcurrencyTests
{
    [Fact]
    public void ConcurrentEnqueue_1000Tasks_AllTasksQueued()
    {
        var queue = new EngineExecutionQueue();

        Parallel.For(0, 1000, i =>
        {
            var task = new EngineExecutionTask($"Engine_{i}", new { Index = i });
            queue.Enqueue(task);
        });

        Assert.Equal(1000, queue.Count());
    }

    [Fact]
    public void ConcurrentDequeue_1000Tasks_AllTasksProcessed()
    {
        var queue = new EngineExecutionQueue();

        for (var i = 0; i < 1000; i++)
        {
            queue.Enqueue(new EngineExecutionTask($"Engine_{i}", new { Index = i }));
        }

        var processed = new ConcurrentBag<string>();

        Parallel.For(0, 1000, _ =>
        {
            var task = queue.Dequeue();
            if (task is not null)
                processed.Add(task.EngineName);
        });

        Assert.Equal(1000, processed.Count);
        Assert.Equal(0, queue.Count());
    }

    [Fact]
    public void ConcurrentDequeue_1000Tasks_NoDuplicateExecution()
    {
        var queue = new EngineExecutionQueue();

        for (var i = 0; i < 1000; i++)
        {
            queue.Enqueue(new EngineExecutionTask($"Engine_{i}", new { Index = i }));
        }

        var processed = new ConcurrentBag<string>();

        Parallel.For(0, 2000, _ =>
        {
            var task = queue.Dequeue();
            if (task is not null)
                processed.Add(task.EngineName);
        });

        var uniqueNames = processed.Distinct().ToList();

        Assert.Equal(1000, processed.Count);
        Assert.Equal(1000, uniqueNames.Count);
    }

    [Fact]
    public async Task ConcurrentEnqueueAndDequeue_NoDataLoss()
    {
        var queue = new EngineExecutionQueue();
        var processed = new ConcurrentBag<string>();

        var enqueueTask = Task.Run(() =>
        {
            for (var i = 0; i < 1000; i++)
            {
                queue.Enqueue(new EngineExecutionTask($"Engine_{i}", new { Index = i }));
            }
        });

        var dequeueTasks = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            while (!enqueueTask.IsCompleted || queue.Count() > 0)
            {
                var task = queue.Dequeue();
                if (task is not null)
                    processed.Add(task.EngineName);
            }
        })).ToArray();

        await Task.WhenAll([enqueueTask, .. dequeueTasks]);

        // Drain any remaining items
        while (true)
        {
            var task = queue.Dequeue();
            if (task is null) break;
            processed.Add(task.EngineName);
        }

        Assert.Equal(1000, processed.Count);
    }

    [Fact]
    public void MultipleWorkers_FetchTasks_NoDuplicates()
    {
        var queue = new EngineExecutionQueue();

        for (var i = 0; i < 1000; i++)
        {
            queue.Enqueue(new EngineExecutionTask($"Engine_{i}", new { Index = i }));
        }

        var workers = Enumerable.Range(0, 8)
            .Select(_ => new EnginePoolWorker(queue))
            .ToList();

        var processed = new ConcurrentBag<string>();

        Parallel.ForEach(workers, worker =>
        {
            while (true)
            {
                var task = worker.FetchTask();
                if (task is null) break;
                processed.Add(task.EngineName);
            }
        });

        var uniqueNames = processed.Distinct().ToList();

        Assert.Equal(1000, processed.Count);
        Assert.Equal(1000, uniqueNames.Count);
    }
}
