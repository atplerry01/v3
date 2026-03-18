namespace Whycespace.PartitionRuntime.Dispatcher;

using Whycespace.CommandSystem.Core.Models;
using Whycespace.Shared.Primitives.Common;
using Whycespace.Contracts.Runtime;
using Whycespace.PartitionRuntime.Models;
using Whycespace.PartitionRuntime.Resolver;
using Whycespace.PartitionRuntime.Router;
using Whycespace.PartitionRuntime.Worker;

public sealed class WorkflowPartitionDispatcher
{
    private readonly IPartitionKeyResolver _resolver;
    private readonly IPartitionRouter _router;
    private readonly PartitionWorkerPool _pool;

    public WorkflowPartitionDispatcher(
        IPartitionKeyResolver resolver,
        IPartitionRouter router,
        PartitionWorkerPool pool)
    {
        _resolver = resolver;
        _router = router;
        _pool = pool;
    }

    public async Task<ExecutionResult> DispatchAsync(
        CommandEnvelope command,
        WorkflowExecutionRequest request,
        CancellationToken cancellationToken)
    {
        var partitionKey = _resolver.Resolve(command);
        var partitionId = _router.ResolvePartition(partitionKey);

        var updatedRequest = request with { PartitionKey = partitionKey };

        var worker = _pool.GetWorker(partitionId);
        return await worker.EnqueueAsync(updatedRequest);
    }

    public PartitionAssignment ResolveAssignment(CommandEnvelope command)
    {
        var partitionKey = _resolver.Resolve(command);
        var partitionId = _router.ResolvePartition(partitionKey);
        return new PartitionAssignment(partitionKey, partitionId);
    }
}
