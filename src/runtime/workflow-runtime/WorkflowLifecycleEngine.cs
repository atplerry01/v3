namespace Whycespace.WorkflowRuntime;

using WorkflowStep = Whycespace.Contracts.Workflows.WorkflowStep;
using Whycespace.Systems.WSS.Registry;
using Whycespace.Runtime.Persistence.Workflow;
using Whycespace.Systems.Midstream.WSS.Events;
using Whycespace.Systems.Midstream.WSS.Models;

/// <summary>
/// Runtime workflow lifecycle orchestrator. Manages workflow start, step advance, completion, failure, and termination.
/// Moved from engine layer to runtime layer as part of WBSM v3 architecture compliance.
/// </summary>
public sealed class WorkflowLifecycleEngine : IWorkflowLifecycleEngine
{
    private readonly IWssWorkflowDefinitionRegistry _workflowRegistry;
    private readonly IWorkflowInstanceRegistry _instanceRegistry;
    private readonly IWssWorkflowStateStore _stateStore;
    private readonly IWorkflowEventRouter _eventRouter;
    private readonly IWorkflowRetryPolicyEngine _retryPolicyEngine;
    private readonly IWorkflowRetryStore _retryStore;
    private readonly IWorkflowTimeoutEngine _timeoutEngine;
    private readonly IWorkflowGraphEngine _graphEngine;

    public WorkflowLifecycleEngine(
        IWssWorkflowDefinitionRegistry workflowRegistry,
        IWorkflowInstanceRegistry instanceRegistry,
        IWssWorkflowStateStore stateStore,
        IWorkflowEventRouter eventRouter,
        IWorkflowRetryPolicyEngine retryPolicyEngine,
        IWorkflowRetryStore retryStore,
        IWorkflowTimeoutEngine timeoutEngine,
        IWorkflowGraphEngine graphEngine)
    {
        _workflowRegistry = workflowRegistry;
        _instanceRegistry = instanceRegistry;
        _stateStore = stateStore;
        _eventRouter = eventRouter;
        _retryPolicyEngine = retryPolicyEngine;
        _retryStore = retryStore;
        _timeoutEngine = timeoutEngine;
        _graphEngine = graphEngine;
    }

    public async Task<LifecycleDecision> StartWorkflow(string workflowId, string version, IDictionary<string, object>? context)
    {
        var definition = _workflowRegistry.GetWorkflow(workflowId);
        var instance = _instanceRegistry.CreateInstance(workflowId, version, context);
        var graph = _graphEngine.BuildGraph(definition);
        var startSteps = _graphEngine.GetStartNodes(graph);
        var firstStep = startSteps.Count > 0 ? startSteps[0] : "";

        var state = new WorkflowState(
            instance.InstanceId, workflowId, version,
            firstStep, new List<string>(),
            WorkflowInstanceStatus.Running,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            context != null ? new Dictionary<string, object>(context) : new Dictionary<string, object>());

        _stateStore.SaveState(state);

        await _eventRouter.PublishEvent("WorkflowStarted", workflowId, instance.InstanceId,
            new Dictionary<string, object> { ["version"] = version, ["firstStep"] = firstStep });

        return new LifecycleDecision(instance.InstanceId, firstStep, null, WorkflowInstanceStatus.Running, "Workflow started");
    }

    public async Task<LifecycleDecision> AdvanceStep(string instanceId)
    {
        var state = _stateStore.GetState(instanceId);
        var definition = _workflowRegistry.GetWorkflow(state.WorkflowId);
        var graph = _graphEngine.BuildGraph(definition);
        var nextSteps = _graphEngine.GetNextNodes(graph, state.CurrentStep);

        if (nextSteps.Count == 0)
            return new LifecycleDecision(instanceId, state.CurrentStep, null, WorkflowInstanceStatus.Completed, "No more steps");

        var nextStep = nextSteps[0];
        _stateStore.UpdateState(instanceId, nextStep, WorkflowInstanceStatus.Running);

        await _eventRouter.PublishEvent("WorkflowStepStarted", state.WorkflowId, instanceId,
            new Dictionary<string, object> { ["stepId"] = nextStep });

        return new LifecycleDecision(instanceId, nextStep, null, WorkflowInstanceStatus.Running, $"Advanced to {nextStep}");
    }

    public async Task<LifecycleDecision> CompleteStep(string instanceId, string stepId)
    {
        _stateStore.AddCompletedStep(instanceId, stepId);
        _timeoutEngine.ClearTimeout(instanceId, stepId);

        await _eventRouter.PublishEvent("WorkflowStepCompleted", "", instanceId,
            new Dictionary<string, object> { ["stepId"] = stepId });

        var state = _stateStore.GetState(instanceId);
        var definition = _workflowRegistry.GetWorkflow(state.WorkflowId);
        var graph = _graphEngine.BuildGraph(definition);
        var nextSteps = _graphEngine.GetNextNodes(graph, stepId);

        var nextStep = nextSteps.Count > 0 ? nextSteps[0] : "";
        return new LifecycleDecision(instanceId, stepId, nextStep, WorkflowInstanceStatus.Running, $"Step {stepId} completed");
    }

    public async Task<LifecycleDecision> FailStep(string instanceId, string stepId, string reason)
    {
        var state = _stateStore.GetState(instanceId);
        var stepDef = GetStepDefinition(state.WorkflowId, stepId);

        if (stepDef != null)
        {
            var command = new WorkflowRetryPolicyCommand(
                instanceId, stepId,
                new Whycespace.Engines.T1M.WSS.Workflows.RetryPolicy(3, Whycespace.Engines.T1M.WSS.Workflows.RetryStrategy.FixedDelay, TimeSpan.FromSeconds(1), 2.0),
                _retryStore.GetRetryCount(instanceId, stepId),
                DateTimeOffset.UtcNow);

            var result = _retryPolicyEngine.EvaluateRetryPolicy(command);

            if (result.RetryAllowed)
            {
                _retryStore.IncrementRetryCount(instanceId, stepId);
                await _eventRouter.PublishEvent("WorkflowStepRetrying", state.WorkflowId, instanceId,
                    new Dictionary<string, object> { ["stepId"] = stepId, ["retryCount"] = result.RetryCount + 1 });
                return new LifecycleDecision(instanceId, stepId, null, WorkflowInstanceStatus.Running, $"Retrying step {stepId}");
            }
        }

        _stateStore.UpdateState(instanceId, stepId, WorkflowInstanceStatus.Failed);
        _instanceRegistry.UpdateInstanceState(instanceId, stepId, WorkflowInstanceStatus.Failed);

        await _eventRouter.PublishEvent("WorkflowStepFailed", state.WorkflowId, instanceId,
            new Dictionary<string, object> { ["stepId"] = stepId, ["reason"] = reason });

        return new LifecycleDecision(instanceId, stepId, null, WorkflowInstanceStatus.Failed, $"Step {stepId} failed: {reason}");
    }

    public async Task<LifecycleDecision> CompleteWorkflow(string instanceId)
    {
        _stateStore.UpdateState(instanceId, "", WorkflowInstanceStatus.Completed);
        _instanceRegistry.UpdateInstanceState(instanceId, "", WorkflowInstanceStatus.Completed);

        await _eventRouter.PublishEvent("WorkflowCompleted", "", instanceId);
        return new LifecycleDecision(instanceId, null, null, WorkflowInstanceStatus.Completed, "Workflow completed");
    }

    public async Task<LifecycleDecision> TerminateWorkflow(string instanceId)
    {
        _stateStore.UpdateState(instanceId, "", WorkflowInstanceStatus.Cancelled);
        _instanceRegistry.UpdateInstanceState(instanceId, "", WorkflowInstanceStatus.Cancelled);

        await _eventRouter.PublishEvent("WorkflowCancelled", "", instanceId);
        return new LifecycleDecision(instanceId, null, null, WorkflowInstanceStatus.Cancelled, "Workflow terminated");
    }

    private WorkflowStep? GetStepDefinition(string workflowId, string stepId)
    {
        try
        {
            var definition = _workflowRegistry.GetWorkflow(workflowId);
            return definition.Steps.FirstOrDefault(s => s.StepId == stepId);
        }
        catch { return null; }
    }

}
