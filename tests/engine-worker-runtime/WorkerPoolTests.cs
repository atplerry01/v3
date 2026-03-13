using Whycespace.EngineRuntime.Registry;
using Whycespace.EngineRuntime.Resolver;
using Whycespace.EngineWorkerRuntime.Pool;
using Whycespace.EngineWorkerRuntime.Queue;

namespace Whycespace.EngineWorkerRuntime.Tests;

public class WorkerPoolTests
{
    [Fact]
    public void Pool_Creates_Correct_Worker_Count()
    {
        var registry = new EngineRegistry();
        var resolver = new EngineResolver(registry);
        var queues = new PartitionEngineQueueRegistry(4);

        var pool = new PartitionEngineWorkerPool(4, 2, resolver, queues);

        Assert.Equal(8, pool.TotalWorkerCount);
        Assert.Equal(2, pool.WorkersPerPartition);
        Assert.Equal(4, pool.Workers.Count);
    }

    [Fact]
    public void Each_Partition_Has_Expected_Workers()
    {
        var registry = new EngineRegistry();
        var resolver = new EngineResolver(registry);
        var queues = new PartitionEngineQueueRegistry(3);

        var pool = new PartitionEngineWorkerPool(3, 3, resolver, queues);

        foreach (var partition in pool.Workers)
        {
            Assert.Equal(3, partition.Value.Count);
        }
    }
}
