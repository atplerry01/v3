namespace Whycespace.WorkerPoolRuntime.Workers;

using Whycespace.WorkerPoolRuntime.Models;
using Whycespace.WorkerPoolRuntime.Queue;

public sealed class EnginePoolWorker
{
    private readonly EngineExecutionQueue _queue;

    public EnginePoolWorker(EngineExecutionQueue queue)
    {
        _queue = queue;
    }

    public EngineExecutionTask? FetchTask()
    {
        return _queue.Dequeue();
    }
}
