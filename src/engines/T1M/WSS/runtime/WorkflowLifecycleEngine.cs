namespace Whycespace.Engines.T1M.WSS.Runtime;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;
using Whycespace.Engines.T1M.WSS.Graph;
using Whycespace.Engines.T1M.WSS.Instance;
using Whycespace.Engines.T1M.WSS.Registry;
using Whycespace.Engines.T1M.WSS.Stores;
using Whycespace.System.Midstream.WSS.Events;
using Whycespace.System.Midstream.WSS.Models;

[EngineManifest("WorkflowInstanceLifecycleEngine", EngineTier.T1M, EngineKind.Decision, "WorkflowLifecycleRequest", typeof(EngineEvent))]
public sealed class WorkflowLifecycleEngine : IEngine, IWorkflowLifecycleEngine
{
    private readonly IWorkflowRegistry _workflowRegistry;
    private readonly IWorkflowInstanceRegistry _instanceRegistry;
    private readonly IWssWorkflowStateStore _stateStore;
    private readonly IWorkflowEventRouter _eventRouter;
    private readonly IWorkflowRetryPolicyEngine _retryPolicyEngine;
    private readonly IWorkflowRetryStore _retryStore;
    private readonly IWorkflowTimeoutEngine _timeoutEngine;
    private readonly IWorkflowGraphEngine _graphEngine;

    public string Name => "WorkflowInstanceLifecycleEngine";

    public WorkflowLifecycleEngine(
        IWorkflowRegistry workflowRegistry,
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

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var action = context.Data.GetValueOrDefault("action") as string;

        return action switch
        {
            "start" => HandleStart(context),
            "advance" => HandleAdvance(context),
            "completeStep" => HandleCompleteStep(context),
            "failStep" => HandleFailStep(context),
            "complete" => HandleComplete(context),
            "terminate" => HandleTerminate(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown action '{action}'. Expected: start, advance, completeStep, failStep, complete, terminate"))
        };
    }

    public async Task<LifecycleDecision> StartWorkflow(string workflowId, string version, IDictionary<string, object>? context)
    {
        var definition = _workflowRegistry.GetWorkflow(workflowId);

        var instance = _instanceRegistry.CreateInstance(workflowId, version, context);

        var stepDefs = definition.Steps.Select(s => new WorkflowStepDefinition(
            s.StepId, s.Name, s.EngineName, "", s.NextSteps, null)).ToList();
        var graph = _graphEngine.BuildGraph(stepDefs);
        var startSteps = _graphEngine.GetStartSteps(graph);
        var firstStep = startSteps.FirstOrDefault() ?? "none";

        var state = new WorkflowState(
            instance.InstanceId,
            workflowId,
            version,
            firstStep,
            Array.Empty<string>(),
            WorkflowInstanceStatus.Running,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            context as IReadOnlyDictionary<string, object> ?? new Dictionary<string, object>(context ?? new Dictionary<string, object>()));

        _stateStore.SaveState(state);
        _instanceRegistry.UpdateInstanceState(instance.InstanceId, firstStep, WorkflowInstanceStatus.Running);

        await _eventRouter.PublishEvent(
            WorkflowEventTypes.WorkflowStarted,
            workflowId,
            instance.InstanceId,
            new Dictionary<string, object>
            {
                ["version"] = version,
                ["firstStep"] = firstStep
            });

        return new LifecycleDecision(instance.InstanceId, null, firstStep, WorkflowInstanceStatus.Running, "Workflow started");
    }

    public async Task<LifecycleDecision> AdvanceStep(string instanceId)
    {
        var state = _stateStore.GetState(instanceId);
        var definition = _workflowRegistry.GetWorkflow(state.WorkflowId);

        var stepDefs = definition.Steps.Select(s => new WorkflowStepDefinition(
            s.StepId, s.Name, s.EngineName, "", s.NextSteps, null)).ToList();
        var graph = _graphEngine.BuildGraph(stepDefs);
        var nextSteps = _graphEngine.GetNextSteps(graph, state.CurrentStep);

        if (nextSteps.Count == 0)
        {
            return await CompleteWorkflow(instanceId);
        }

        var nextStep = nextSteps[0];
        _stateStore.UpdateState(instanceId, nextStep, WorkflowInstanceStatus.Running);
        _instanceRegistry.UpdateInstanceState(instanceId, nextStep, WorkflowInstanceStatus.Running);

        await _eventRouter.PublishEvent(
            WorkflowEventTypes.WorkflowStepStarted,
            state.WorkflowId,
            instanceId,
            new Dictionary<string, object>
            {
                ["stepId"] = nextStep,
                ["previousStep"] = state.CurrentStep
            });

        return new LifecycleDecision(instanceId, state.CurrentStep, nextStep, WorkflowInstanceStatus.Running, "Step advanced");
    }

    public async Task<LifecycleDecision> CompleteStep(string instanceId, string stepId)
    {
        var state = _stateStore.GetState(instanceId);

        _stateStore.AddCompletedStep(instanceId, stepId);
        _timeoutEngine.ClearTimeout(instanceId, stepId);

        await _eventRouter.PublishEvent(
            WorkflowEventTypes.WorkflowStepCompleted,
            state.WorkflowId,
            instanceId,
            new Dictionary<string, object>
            {
                ["stepId"] = stepId
            });

        var definition = _workflowRegistry.GetWorkflow(state.WorkflowId);
        var stepDefs = definition.Steps.Select(s => new WorkflowStepDefinition(
            s.StepId, s.Name, s.EngineName, "", s.NextSteps, null)).ToList();
        var graph = _graphEngine.BuildGraph(stepDefs);
        var nextSteps = _graphEngine.GetNextSteps(graph, stepId);

        if (nextSteps.Count == 0)
        {
            return new LifecycleDecision(instanceId, stepId, null, WorkflowInstanceStatus.Running, "Step completed, no more steps");
        }

        var nextStep = nextSteps[0];
        _stateStore.UpdateState(instanceId, nextStep, WorkflowInstanceStatus.Running);
        _instanceRegistry.UpdateInstanceState(instanceId, nextStep, WorkflowInstanceStatus.Running);

        return new LifecycleDecision(instanceId, stepId, nextStep, WorkflowInstanceStatus.Running, "Step completed");
    }

    public async Task<LifecycleDecision> FailStep(string instanceId, string stepId, string reason)
    {
        var state = _stateStore.GetState(instanceId);

        var retryCount = _retryStore.GetRetryCount(instanceId, stepId);

        var stepDef = GetStepDefinition(state.WorkflowId, stepId);
        var failurePolicy = stepDef?.FailurePolicy ?? new WorkflowFailurePolicy(FailureAction.Fail, 0, TimeSpan.Zero, null);

        var retryCommand = new WorkflowRetryPolicyCommand(
            instanceId,
            stepId,
            new Domain.Core.Workflows.RetryPolicy(
                failurePolicy.MaxRetries,
                Domain.Core.Workflows.RetryStrategy.FixedDelay,
                failurePolicy.RetryDelay,
                1.0),
            retryCount,
            DateTimeOffset.UtcNow);

        var retryResult = _retryPolicyEngine.EvaluateRetryPolicy(retryCommand);

        if (retryResult.RetryAllowed)
        {
            _retryStore.IncrementRetryCount(instanceId, stepId);

            return new LifecycleDecision(instanceId, stepId, stepId, WorkflowInstanceStatus.Running, $"Retrying step (attempt {retryCount + 1})");
        }

        _stateStore.UpdateState(instanceId, stepId, WorkflowInstanceStatus.Failed);
        _instanceRegistry.UpdateInstanceState(instanceId, stepId, WorkflowInstanceStatus.Failed);

        await _eventRouter.PublishEvent(
            WorkflowEventTypes.WorkflowStepFailed,
            state.WorkflowId,
            instanceId,
            new Dictionary<string, object>
            {
                ["stepId"] = stepId,
                ["reason"] = reason,
                ["retryCount"] = retryCount
            });

        return new LifecycleDecision(instanceId, stepId, null, WorkflowInstanceStatus.Failed, reason);
    }

    public async Task<LifecycleDecision> CompleteWorkflow(string instanceId)
    {
        var state = _stateStore.GetState(instanceId);

        _stateStore.UpdateState(instanceId, state.CurrentStep, WorkflowInstanceStatus.Completed);
        _instanceRegistry.UpdateInstanceState(instanceId, state.CurrentStep, WorkflowInstanceStatus.Completed);

        await _eventRouter.PublishEvent(
            WorkflowEventTypes.WorkflowCompleted,
            state.WorkflowId,
            instanceId,
            new Dictionary<string, object>
            {
                ["completedSteps"] = state.CompletedSteps.Count
            });

        return new LifecycleDecision(instanceId, state.CurrentStep, null, WorkflowInstanceStatus.Completed, "Workflow completed");
    }

    public async Task<LifecycleDecision> TerminateWorkflow(string instanceId)
    {
        var state = _stateStore.GetState(instanceId);

        _stateStore.UpdateState(instanceId, state.CurrentStep, WorkflowInstanceStatus.Cancelled);
        _instanceRegistry.UpdateInstanceState(instanceId, state.CurrentStep, WorkflowInstanceStatus.Cancelled);

        await _eventRouter.PublishEvent(
            WorkflowEventTypes.WorkflowCancelled,
            state.WorkflowId,
            instanceId,
            new Dictionary<string, object>
            {
                ["cancelledAtStep"] = state.CurrentStep
            });

        return new LifecycleDecision(instanceId, state.CurrentStep, null, WorkflowInstanceStatus.Cancelled, "Workflow terminated");
    }

    private WorkflowStepDefinition? GetStepDefinition(string workflowId, string stepId)
    {
        var definition = _workflowRegistry.GetWorkflow(workflowId);
        var step = definition.Steps.FirstOrDefault(s => s.StepId == stepId);
        if (step is null) return null;
        return new WorkflowStepDefinition(step.StepId, step.Name, step.EngineName, "", step.NextSteps, null);
    }

    private async Task<EngineResult> HandleStart(EngineContext context)
    {
        var workflowId = context.Data.GetValueOrDefault("workflowId") as string;
        var version = context.Data.GetValueOrDefault("version") as string;

        if (string.IsNullOrWhiteSpace(workflowId) || string.IsNullOrWhiteSpace(version))
            return EngineResult.Fail("Missing workflowId or version");

        var ctx = context.Data
            .Where(kv => kv.Key is not "action" and not "workflowId" and not "version")
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var decision = await StartWorkflow(workflowId, version, ctx);

        return EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["instanceId"] = decision.InstanceId,
            ["nextStep"] = decision.NextStep ?? "",
            ["status"] = decision.Status.ToString()
        });
    }

    private async Task<EngineResult> HandleAdvance(EngineContext context)
    {
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string;

        if (string.IsNullOrWhiteSpace(instanceId))
            return EngineResult.Fail("Missing instanceId");

        var decision = await AdvanceStep(instanceId);

        return EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["instanceId"] = decision.InstanceId,
            ["currentStep"] = decision.CurrentStep ?? "",
            ["nextStep"] = decision.NextStep ?? "",
            ["status"] = decision.Status.ToString()
        });
    }

    private async Task<EngineResult> HandleCompleteStep(EngineContext context)
    {
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string;
        var stepId = context.Data.GetValueOrDefault("stepId") as string;

        if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(stepId))
            return EngineResult.Fail("Missing instanceId or stepId");

        var decision = await CompleteStep(instanceId, stepId);

        return EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["instanceId"] = decision.InstanceId,
            ["currentStep"] = decision.CurrentStep ?? "",
            ["nextStep"] = decision.NextStep ?? "",
            ["status"] = decision.Status.ToString()
        });
    }

    private async Task<EngineResult> HandleFailStep(EngineContext context)
    {
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string;
        var stepId = context.Data.GetValueOrDefault("stepId") as string;
        var reason = context.Data.GetValueOrDefault("reason") as string ?? "Unknown";

        if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(stepId))
            return EngineResult.Fail("Missing instanceId or stepId");

        var decision = await FailStep(instanceId, stepId, reason);

        return EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["instanceId"] = decision.InstanceId,
            ["currentStep"] = decision.CurrentStep ?? "",
            ["nextStep"] = decision.NextStep ?? "",
            ["status"] = decision.Status.ToString(),
            ["reason"] = decision.Reason ?? ""
        });
    }

    private async Task<EngineResult> HandleComplete(EngineContext context)
    {
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string;

        if (string.IsNullOrWhiteSpace(instanceId))
            return EngineResult.Fail("Missing instanceId");

        var decision = await CompleteWorkflow(instanceId);

        return EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["instanceId"] = decision.InstanceId,
            ["status"] = decision.Status.ToString()
        });
    }

    private async Task<EngineResult> HandleTerminate(EngineContext context)
    {
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string;

        if (string.IsNullOrWhiteSpace(instanceId))
            return EngineResult.Fail("Missing instanceId");

        var decision = await TerminateWorkflow(instanceId);

        return EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["instanceId"] = decision.InstanceId,
            ["status"] = decision.Status.ToString()
        });
    }
}
