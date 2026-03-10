namespace Whycespace.RuntimeDispatcher.Dispatcher;

using Whycespace.CommandSystem.Idempotency;
using Whycespace.CommandSystem.Models;
using Whycespace.CommandSystem.Validation;
using Whycespace.Contracts.Primitives;
using Whycespace.Contracts.Runtime;
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
        WfRuntime runtime)
    {
        _pipeline = new ExecutionPipeline(validator, idempotency, resolver, runtime);
    }

    public async Task<ExecutionResult> DispatchAsync(
        CommandEnvelope command,
        CancellationToken cancellationToken)
    {
        return await _pipeline.ExecuteAsync(command, cancellationToken);
    }
}
