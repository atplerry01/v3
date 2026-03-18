namespace Whycespace.RuntimeDispatcher.Pipeline;

using Whycespace.CommandSystem.Core.Idempotency;
using Whycespace.CommandSystem.Core.Models;
using Whycespace.CommandSystem.Core.Validation;
using Whycespace.Shared.Primitives.Common;
using Whycespace.Contracts.Runtime;
using Whycespace.PartitionRuntime.Dispatcher;
using Whycespace.Runtime.CommandRouting;
using Whycespace.Runtime.ControlPlane;
using Whycespace.Runtime.EventFabricGuard;
using Whycespace.RuntimeDispatcher.Resolver;
using WfRuntime = Whycespace.WorkflowRuntime.Runtime.WorkflowRuntime;

public sealed class ControlPlaneExecutionPipeline
{
    private readonly ICommandValidator _validator;
    private readonly IIdempotencyRegistry _idempotency;
    private readonly IWorkflowResolver _workflowResolver;
    private readonly WfRuntime _runtime;
    private readonly WorkflowPartitionDispatcher? _partitionDispatcher;
    private readonly RuntimeControlPlane _controlPlane;
    private readonly CommandRouteRegistry? _routeRegistry;
    private readonly EventFabricGuard? _eventGuard;

    public ControlPlaneExecutionPipeline(
        ICommandValidator validator,
        IIdempotencyRegistry idempotency,
        IWorkflowResolver workflowResolver,
        WfRuntime runtime,
        RuntimeControlPlane controlPlane,
        CommandRouteRegistry? routeRegistry = null,
        EventFabricGuard? eventGuard = null,
        WorkflowPartitionDispatcher? partitionDispatcher = null)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _idempotency = idempotency ?? throw new ArgumentNullException(nameof(idempotency));
        _workflowResolver = workflowResolver ?? throw new ArgumentNullException(nameof(workflowResolver));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _controlPlane = controlPlane ?? throw new ArgumentNullException(nameof(controlPlane));
        _routeRegistry = routeRegistry;
        _eventGuard = eventGuard;
        _partitionDispatcher = partitionDispatcher;
    }

    public async Task<ExecutionResult> ExecuteAsync(
        CommandEnvelope command,
        CancellationToken cancellationToken)
    {
        // Step 1: Validate command
        ValidateCommand(command);

        // Step 2: Verify command is registered in control plane
        ValidateCommandRegistration(command);

        // Step 3: Check idempotency before engine invocation
        CheckIdempotency(command);

        // Step 4: Resolve engine via command routing (if available)
        ResolveEngineRoute(command);

        // Step 5: Resolve workflow
        var workflowName = ResolveWorkflow(command);

        // Step 6: Execute workflow (engine execution happens within)
        var result = await ExecuteWorkflow(workflowName, command, cancellationToken);

        return result;
    }

    private void ValidateCommand(CommandEnvelope command)
    {
        _validator.Validate(command);
    }

    private void ValidateCommandRegistration(CommandEnvelope command)
    {
        if (!_controlPlane.Commands.IsRegistered(command.CommandType))
            return;
    }

    private void CheckIdempotency(CommandEnvelope command)
    {
        if (_idempotency.Exists(command.CommandId))
            throw new InvalidOperationException($"Duplicate command: {command.CommandId}");

        _idempotency.Register(command.CommandId);
    }

    private void ResolveEngineRoute(CommandEnvelope command)
    {
        if (_routeRegistry is null)
            return;

        if (!_routeRegistry.HasRoute(command.CommandType))
            return;

        var engineId = _routeRegistry.ResolveEngine(command.CommandType);
        _ = _controlPlane.Engines.GetById(engineId);
    }

    private string ResolveWorkflow(CommandEnvelope command)
    {
        return _workflowResolver.ResolveWorkflow(command.CommandType);
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
