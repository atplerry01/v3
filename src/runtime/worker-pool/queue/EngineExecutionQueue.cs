namespace Whycespace.WorkerPoolRuntime.Queue;

using Whycespace.WorkerPoolRuntime.Models;

public sealed class EngineExecutionQueue
{
    private readonly Queue<EngineExecutionTask> _queue = new();

    private readonly object _lock = new();

    public void Enqueue(EngineExecutionTask task)
    {
        lock (_lock)
        {
            _queue.Enqueue(task);
        }
    }

    public EngineExecutionTask? Dequeue()
    {
        lock (_lock)
        {
            if (_queue.Count == 0)
                return null;

            return _queue.Dequeue();
        }
    }

    public int Count()
    {
        lock (_lock)
        {
            return _queue.Count;
        }
    }
}
