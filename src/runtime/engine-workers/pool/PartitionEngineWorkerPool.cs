using Whycespace.EngineRuntime.Resolver;
using Whycespace.EngineWorkerRuntime.Queue;
using Whycespace.EngineWorkerRuntime.Worker;

namespace Whycespace.EngineWorkerRuntime.Pool;

public sealed class PartitionEngineWorkerPool
{
    private readonly Dictionary<int, List<EngineWorker>> _workers;
    private readonly int _workersPerPartition;

    public PartitionEngineWorkerPool(
        int partitionCount,
        int workersPerPartition,
        EngineResolver resolver,
        PartitionEngineQueueRegistry queues)
    {
        _workersPerPartition = workersPerPartition;
        _workers = new Dictionary<int, List<EngineWorker>>();

        int globalId = 0;

        for (int partition = 0; partition < partitionCount; partition++)
        {
            var list = new List<EngineWorker>();

            for (int i = 0; i < workersPerPartition; i++)
            {
                var reader = queues.GetReader(partition);
                list.Add(new EngineWorker(globalId++, partition, resolver, reader));
            }

            _workers[partition] = list;
        }
    }

    public IReadOnlyDictionary<int, List<EngineWorker>> Workers => _workers;
    public int WorkersPerPartition => _workersPerPartition;
    public int TotalWorkerCount => _workers.Values.Sum(l => l.Count);
}
