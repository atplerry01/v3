using Whycespace.EngineWorkerRuntime.Pool;

namespace Whycespace.EngineWorkerRuntime.Supervisor;

public sealed class EngineWorkerSupervisor
{
    private readonly PartitionEngineWorkerPool _pool;
    private readonly List<Task> _tasks = new();

    public EngineWorkerSupervisor(PartitionEngineWorkerPool pool)
    {
        _pool = pool;
    }

    public void Start(CancellationToken cancellationToken)
    {
        foreach (var partition in _pool.Workers.Values)
        {
            foreach (var worker in partition)
            {
                _tasks.Add(Task.Run(() => worker.RunAsync(cancellationToken), cancellationToken));
            }
        }
    }

    public int RunningWorkerCount => _pool.Workers.Values
        .SelectMany(w => w)
        .Count(w => w.IsRunning);
}
