namespace Whycespace.WorkerPoolRuntime.Pool;

using Whycespace.WorkerPoolRuntime.Workers;

public sealed class EngineWorkerPoolManager
{
    private readonly EngineWorkerPool _pool;

    public EngineWorkerPoolManager(EngineWorkerPool pool)
    {
        _pool = pool;
    }

    public IReadOnlyCollection<EnginePoolWorker> GetWorkers()
    {
        return _pool.Workers();
    }
}
