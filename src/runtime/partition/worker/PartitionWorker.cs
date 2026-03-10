namespace Whycespace.PartitionRuntime.Worker;

using System.Threading.Channels;
using Whycespace.Contracts.Runtime;
using WfRuntime = Whycespace.WorkflowRuntime.Runtime.WorkflowRuntime;

public sealed class PartitionWorker
{
    private readonly int _partitionId;
    private readonly WfRuntime _runtime;
    private readonly Channel<TrackedRequest> _queue;
    private readonly Task _processingTask;

    public PartitionWorker(int partitionId, WfRuntime runtime)
    {
        _partitionId = partitionId;
        _runtime = runtime;
        _queue = Channel.CreateUnbounded<TrackedRequest>();
        _processingTask = Task.Run(ProcessQueueAsync);
    }

    public int PartitionId => _partitionId;

    public async Task<ExecutionResult> EnqueueAsync(WorkflowExecutionRequest request)
    {
        var tcs = new TaskCompletionSource<ExecutionResult>();
        var wrapped = new TrackedRequest(request, tcs);

        await _queue.Writer.WriteAsync(wrapped);

        return await tcs.Task;
    }

    private async Task ProcessQueueAsync()
    {
        await foreach (var tracked in _queue.Reader.ReadAllAsync())
        {
            try
            {
                var result = await _runtime.ExecuteAsync(tracked.Request);
                tracked.Completion.SetResult(result);
            }
            catch (Exception ex)
            {
                tracked.Completion.SetException(ex);
            }
        }
    }

    private sealed record TrackedRequest(
        WorkflowExecutionRequest Request,
        TaskCompletionSource<ExecutionResult> Completion
    );
}
