namespace Whycespace.WorkerPoolRuntime.Pool;

using Whycespace.WorkerPoolRuntime.Queue;
using Whycespace.WorkerPoolRuntime.Workers;

public sealed class EngineWorkerPool
{
    private readonly List<EnginePoolWorker> _workers;

    public EngineWorkerPool(int workerCount, EngineExecutionQueue queue)
    {
        _workers = new List<EnginePoolWorker>();

        for (var i = 0; i < workerCount; i++)
        {
            _workers.Add(new EnginePoolWorker(queue));
        }
    }

    public IReadOnlyCollection<EnginePoolWorker> Workers()
    {
        return _workers.AsReadOnly();
    }
}
