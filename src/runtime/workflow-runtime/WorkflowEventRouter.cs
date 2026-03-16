namespace Whycespace.WorkflowRuntime;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Midstream.WSS.Instances;
using Whycespace.Systems.Midstream.WSS.Stores;
using WorkflowInstanceStatus = Whycespace.Systems.Midstream.WSS.Models.WorkflowInstanceStatus;
using StepStatus = Whycespace.Domain.Core.Workflows.StepStatus;

public sealed class WorkflowEventRouter
{
    private readonly IWorkflowInstanceRegistry _instanceRegistry;
    private readonly IWorkflowStateStore _stateStore;
    private readonly ConcurrentDictionary<Guid, byte> _processedEvents = new();

    public WorkflowEventRouter(
        IWorkflowInstanceRegistry instanceRegistry,
        IWorkflowStateStore stateStore)
    {
        _instanceRegistry = instanceRegistry ?? throw new ArgumentNullException(nameof(instanceRegistry));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    }

    public async Task<WorkflowEventRouteResult> RouteAsync(WorkflowEventRouteCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_processedEvents.TryAdd(command.EventId, 0))
            return WorkflowEventRouteResult.Ignored(command.EventId);

        var instances = ResolveAffectedInstances(command);

        if (instances.Count == 0)
            return WorkflowEventRouteResult.Ignored(command.EventId);

        WorkflowEventRouteResult? lastResult = null;

        foreach (var instance in instances)
        {
            var result = await RouteToInstanceAsync(command, instance);
            lastResult = result;
        }

        return lastResult!;
    }

    public async Task<IReadOnlyList<WorkflowEventRouteResult>> RouteBatchAsync(
        IReadOnlyList<WorkflowEventRouteCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        var results = new List<WorkflowEventRouteResult>(commands.Count);

        foreach (var command in commands)
        {
            var result = await RouteAsync(command);
            results.Add(result);
        }

        return results;
    }

    private List<WorkflowInstanceRecord> ResolveAffectedInstances(WorkflowEventRouteCommand command)
    {
        if (!string.IsNullOrEmpty(command.WorkflowCorrelationId))
        {
            var byCorrelation = _instanceRegistry.ResolveByCorrelationId(command.WorkflowCorrelationId);
            if (byCorrelation is not null)
                return [byCorrelation];
        }

        var activeInstances = _instanceRegistry.ListActiveWorkflowInstances();
        return activeInstances
            .Where(i => MatchesAggregate(i, command.AggregateId))
            .ToList();
    }

    private static bool MatchesAggregate(WorkflowInstanceRecord instance, Guid aggregateId)
    {
        return instance.CorrelationId == aggregateId.ToString();
    }

    private Task<WorkflowEventRouteResult> RouteToInstanceAsync(
        WorkflowEventRouteCommand command,
        WorkflowInstanceRecord instance)
    {
        if (instance.Status is WorkflowInstanceStatus.Completed
            or WorkflowInstanceStatus.Failed
            or WorkflowInstanceStatus.Terminated)
        {
            return Task.FromResult(WorkflowEventRouteResult.Ignored(command.EventId));
        }

        var state = _stateStore.GetWorkflowState(instance.InstanceId);

        if (state is null)
            return Task.FromResult(WorkflowEventRouteResult.Failed(command.EventId, instance.InstanceId));

        var affectedStep = ResolveAffectedStep(command, state);

        if (affectedStep is null)
            return Task.FromResult(WorkflowEventRouteResult.Ignored(command.EventId));

        _stateStore.UpdateStepState(instance.InstanceId, affectedStep, StepStatus.Completed);

        _instanceRegistry.UpdateWorkflowInstanceStatus(
            instance.InstanceId, WorkflowInstanceStatus.Running);

        return Task.FromResult(WorkflowEventRouteResult.Matched(
            instance.InstanceId,
            command.EventId,
            affectedStep));
    }

    private static string? ResolveAffectedStep(
        WorkflowEventRouteCommand command,
        WorkflowStateRecord state)
    {
        if (command.Payload.TryGetValue("targetStep", out var stepObj) && stepObj is string targetStep)
            return targetStep;

        if (command.Payload.TryGetValue("completedStep", out var completedObj) && completedObj is string completedStep)
        {
            if (state.CurrentStep == completedStep)
                return completedStep;
        }

        return state.CurrentStep;
    }
}
