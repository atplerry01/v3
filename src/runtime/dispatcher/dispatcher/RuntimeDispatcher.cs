namespace Whycespace.RuntimeDispatcher.Dispatcher;

using Whycespace.CommandSystem.Core.Idempotency;
using Whycespace.CommandSystem.Core.Models;
using Whycespace.CommandSystem.Core.Validation;
using Whycespace.Shared.Primitives.Common;
using Whycespace.Contracts.Runtime;
using Whycespace.PartitionRuntime.Dispatcher;
using Whycespace.RuntimeDispatcher.Pipeline;
using Whycespace.RuntimeDispatcher.Resolver;
using WfRuntime = Whycespace.WorkflowRuntime.Runtime.WorkflowRuntime;

public sealed class RuntimeDispatcher : IRuntimeDispatcher
{
    private readonly ExecutionPipeline _pipeline;

    public RuntimeDispatcher(
        ICommandValidator validator,
        IIdempotencyRegistry idempotency,
        IWorkflowResolver resolver,
        WfRuntime runtime,
        WorkflowPartitionDispatcher? partitionDispatcher = null)
    {
        _pipeline = new ExecutionPipeline(validator, idempotency, resolver, runtime, partitionDispatcher);
    }

    public async Task<ExecutionResult> DispatchAsync(
        CommandEnvelope command,
        CancellationToken cancellationToken)
    {
        return await _pipeline.ExecuteAsync(command, cancellationToken);
    }
}
