namespace Whycespace.RuntimeDispatcher.Pipeline;

using Whycespace.CommandSystem.Idempotency;
using Whycespace.CommandSystem.Models;
using Whycespace.CommandSystem.Validation;
using Whycespace.Contracts.Primitives;
using Whycespace.Contracts.Runtime;
using Whycespace.PartitionRuntime.Dispatcher;
using Whycespace.RuntimeDispatcher.Resolver;
using WfRuntime = Whycespace.WorkflowRuntime.Runtime.WorkflowRuntime;

public sealed class ExecutionPipeline
{
    private readonly ICommandValidator _validator;
    private readonly IIdempotencyRegistry _idempotency;
    private readonly IWorkflowResolver _resolver;
    private readonly WfRuntime _runtime;
    private readonly WorkflowPartitionDispatcher? _partitionDispatcher;

    public ExecutionPipeline(
        ICommandValidator validator,
        IIdempotencyRegistry idempotency,
        IWorkflowResolver resolver,
        WfRuntime runtime,
        WorkflowPartitionDispatcher? partitionDispatcher = null)
    {
        _validator = validator;
        _idempotency = idempotency;
        _resolver = resolver;
        _runtime = runtime;
        _partitionDispatcher = partitionDispatcher;
    }

    public async Task<ExecutionResult> ExecuteAsync(
        CommandEnvelope command,
        CancellationToken cancellationToken)
    {
        ValidateCommand(command);
        CheckIdempotency(command);
        var workflowName = ResolveWorkflow(command);
        return await ExecuteWorkflow(workflowName, command, cancellationToken);
    }

    private void ValidateCommand(CommandEnvelope command)
    {
        _validator.Validate(command);
    }

    private void CheckIdempotency(CommandEnvelope command)
    {
        if (_idempotency.Exists(command.CommandId))
            throw new InvalidOperationException($"Duplicate command: {command.CommandId}");

        _idempotency.Register(command.CommandId);
    }

    private string ResolveWorkflow(CommandEnvelope command)
    {
        return _resolver.ResolveWorkflow(command.CommandType);
    }

    private async Task<ExecutionResult> ExecuteWorkflow(
        string workflowName,
        CommandEnvelope command,
        CancellationToken cancellationToken)
    {
        var request = new WorkflowExecutionRequest(
            WorkflowName: workflowName,
            Context: command.Payload,
            CorrelationId: command.CommandId.ToString(),
            ScheduledAt: null,
            PartitionKey: new PartitionKey(command.CommandId.ToString())
        );

        if (_partitionDispatcher is not null)
            return await _partitionDispatcher.DispatchAsync(command, request, cancellationToken);

        return await _runtime.ExecuteAsync(request);
    }
}
