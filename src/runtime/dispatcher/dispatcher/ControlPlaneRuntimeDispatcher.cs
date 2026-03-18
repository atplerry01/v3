namespace Whycespace.RuntimeDispatcher.Dispatcher;

using Whycespace.CommandSystem.Core.Idempotency;
using Whycespace.CommandSystem.Core.Models;
using Whycespace.CommandSystem.Core.Validation;
using Whycespace.Contracts.Runtime;
using Whycespace.PartitionRuntime.Dispatcher;
using Whycespace.Runtime.CommandRouting;
using Whycespace.Runtime.ControlPlane;
using Whycespace.Runtime.EventFabricGuard;
using Whycespace.RuntimeDispatcher.Pipeline;
using Whycespace.RuntimeDispatcher.Resolver;
using WfRuntime = Whycespace.WorkflowRuntime.Runtime.WorkflowRuntime;

public sealed class ControlPlaneRuntimeDispatcher : IRuntimeDispatcher
{
    private readonly ControlPlaneExecutionPipeline _pipeline;

    public ControlPlaneRuntimeDispatcher(
        ICommandValidator validator,
        IIdempotencyRegistry idempotency,
        IWorkflowResolver resolver,
        WfRuntime runtime,
        RuntimeControlPlane controlPlane,
        CommandRouteRegistry? routeRegistry = null,
        EventFabricGuard? eventGuard = null,
        WorkflowPartitionDispatcher? partitionDispatcher = null)
    {
        _pipeline = new ControlPlaneExecutionPipeline(
            validator,
            idempotency,
            resolver,
            runtime,
            controlPlane,
            routeRegistry,
            eventGuard,
            partitionDispatcher);
    }

    public async Task<ExecutionResult> DispatchAsync(
        CommandEnvelope command,
        CancellationToken cancellationToken)
    {
        return await _pipeline.ExecuteAsync(command, cancellationToken);
    }
}
