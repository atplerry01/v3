namespace Whycespace.PartitionRuntime.Worker;

using WfRuntime = Whycespace.WorkflowRuntime.Runtime.WorkflowRuntime;

public sealed class PartitionWorkerPool
{
    private readonly Dictionary<int, PartitionWorker> _workers;

    public PartitionWorkerPool(int partitionCount, WfRuntime runtime)
    {
        _workers = new Dictionary<int, PartitionWorker>();

        for (int i = 0; i < partitionCount; i++)
        {
            _workers[i] = new PartitionWorker(i, runtime);
        }
    }

    public PartitionWorker GetWorker(int partitionId)
    {
        if (!_workers.TryGetValue(partitionId, out var worker))
            throw new InvalidOperationException($"No worker for partition: {partitionId}");

        return worker;
    }

    public IReadOnlyList<int> GetActivePartitions() =>
        _workers.Keys.OrderBy(k => k).ToList();

    public int PartitionCount => _workers.Count;
}
