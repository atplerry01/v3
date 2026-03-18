using Whycespace.Contracts.Workflows;
using Whycespace.Engines.T1M.Orchestration.Dispatcher;
using Whycespace.Engines.T1M.Orchestration.Resilience;
using Whycespace.Engines.T1M.Orchestration.Scheduling;
using Whycespace.Engines.T1M.WSS.Graph;
using Whycespace.Engines.T1M.WSS.Registry;
using Whycespace.Engines.T1M.Shared;
using Whycespace.Runtime.Persistence.Workflow;
using Whycespace.Systems.Midstream.WSS.Events;
using WorkflowDefinition = Whycespace.Systems.Midstream.WSS.Models.WorkflowDefinition;
using WorkflowInstanceStatus = Whycespace.Systems.Midstream.WSS.Models.WorkflowInstanceStatus;

namespace Whycespace.WSS.WorkflowLifecycle.Tests;

internal sealed class StubWorkflowEventRouter : IWorkflowEventRouter
{
    public List<(string EventType, string WorkflowId, string InstanceId)> PublishedEvents { get; } = new();

    public Task PublishEvent(string eventType, string workflowId, string instanceId, IDictionary<string, object>? payload = null)
    {
        PublishedEvents.Add((eventType, workflowId, instanceId));
        return Task.CompletedTask;
    }

    public void Subscribe(string eventType, Func<WorkflowEventEnvelope, Task> handler) { }

    public Task RouteInternalEvent(WorkflowEventEnvelope envelope) => Task.CompletedTask;
}

public class WorkflowLifecycleEngineTests
{
    private readonly WorkflowLifecycleEngine _engine;
    private readonly WorkflowRegistry _workflowRegistry;
    private readonly WorkflowInstanceRegistry _instanceRegistry;
    private readonly WssWorkflowStateStore _stateStore;
    private readonly StubWorkflowEventRouter _eventRouter;
    private readonly WorkflowRetryPolicyEngine _retryPolicyEngine;
    private readonly WorkflowTimeoutEngine _timeoutEngine;
    private readonly WorkflowGraphEngine _graphEngine;

    public WorkflowLifecycleEngineTests()
    {
        _workflowRegistry = new WorkflowRegistry();
        _instanceRegistry = new WorkflowInstanceRegistry(new InstanceRegistryStoreAdapter());
        _stateStore = new WssWorkflowStateStore();
        _eventRouter = new StubWorkflowEventRouter();

        var retryStore = new WorkflowRetryStore();
        _retryPolicyEngine = new WorkflowRetryPolicyEngine(retryStore);

        var timeoutStore = new WorkflowTimeoutStore();
        _timeoutEngine = new WorkflowTimeoutEngine(timeoutStore);

        _graphEngine = new WorkflowGraphEngine();

        _engine = new WorkflowLifecycleEngine(
            _workflowRegistry,
            _instanceRegistry,
            _stateStore,
            _eventRouter,
            _retryPolicyEngine,
            retryStore,
            _timeoutEngine,
            _graphEngine);

        RegisterTestWorkflow();
    }

    private void RegisterTestWorkflow()
    {
        var steps = new List<WorkflowStep>
        {
            new("validate-passenger", "Validate Passenger", "ValidationEngine", new[] { "find-driver" }),
            new("find-driver", "Find Driver", "MatchingEngine", new[] { "charge-payment" }),
            new("charge-payment", "Charge Payment", "PaymentEngine", Array.Empty<string>())
        };

        var definition = new WorkflowDefinition(
            "taxi-request",
            "Taxi Request Workflow",
            "End-to-end taxi request processing",
            "1.0.0",
            steps,
            DateTimeOffset.UtcNow);

        _workflowRegistry.RegisterWorkflow(definition);
    }

    // 1. Start workflow instance
    [Fact]
    public async Task StartWorkflow_ShouldCreateInstanceAndReturnDecision()
    {
        var decision = await _engine.StartWorkflow("taxi-request", "1.0.0", new Dictionary<string, object>
        {
            ["passenger"] = "user-123"
        });

        Assert.NotNull(decision);
        Assert.Equal(WorkflowInstanceStatus.Running, decision.Status);
        Assert.Equal("validate-passenger", decision.NextStep);
        Assert.NotEmpty(decision.InstanceId);
    }

    // 2. Advance workflow step
    [Fact]
    public async Task AdvanceStep_ShouldMoveToNextStep()
    {
        var startDecision = await _engine.StartWorkflow("taxi-request", "1.0.0", null);

        var decision = await _engine.AdvanceStep(startDecision.InstanceId);

        Assert.Equal(WorkflowInstanceStatus.Running, decision.Status);
        Assert.Equal("validate-passenger", decision.CurrentStep);
        Assert.Equal("find-driver", decision.NextStep);
    }

    // 3. Complete workflow step
    [Fact]
    public async Task CompleteStep_ShouldRecordCompletionAndAdvance()
    {
        var startDecision = await _engine.StartWorkflow("taxi-request", "1.0.0", null);

        var decision = await _engine.CompleteStep(startDecision.InstanceId, "validate-passenger");

        Assert.Equal(WorkflowInstanceStatus.Running, decision.Status);
        Assert.Equal("validate-passenger", decision.CurrentStep);
        Assert.Equal("find-driver", decision.NextStep);

        var state = _stateStore.GetState(startDecision.InstanceId);
        Assert.Contains("validate-passenger", state.CompletedSteps);
    }

    // 4. Handle step failure
    [Fact]
    public async Task FailStep_WhenNoRetryPolicy_ShouldFailWorkflow()
    {
        var startDecision = await _engine.StartWorkflow("taxi-request", "1.0.0", null);

        var decision = await _engine.FailStep(startDecision.InstanceId, "validate-passenger", "Validation error");

        Assert.Equal(WorkflowInstanceStatus.Failed, decision.Status);
        Assert.Equal("Validation error", decision.Reason);
        Assert.Null(decision.NextStep);
    }

    // 5. Retry step (verifies fail path with no retries configured)
    [Fact]
    public async Task FailStep_WithDefaultPolicy_ShouldFailWhenNoRetriesConfigured()
    {
        var stepsWithRetry = new List<WorkflowStep>
        {
            new("validate", "Validate", "ValidationEngine", new[] { "process" }),
            new("process", "Process", "ProcessEngine", Array.Empty<string>())
        };

        var definition = new WorkflowDefinition(
            "retry-workflow",
            "Retry Workflow",
            "Workflow with retry policy",
            "1.0.0",
            stepsWithRetry,
            DateTimeOffset.UtcNow);

        _workflowRegistry.RegisterWorkflow(definition);

        var startDecision = await _engine.StartWorkflow("retry-workflow", "1.0.0", null);

        var decision = await _engine.FailStep(startDecision.InstanceId, "validate", "Temporary error");

        Assert.Equal(WorkflowInstanceStatus.Failed, decision.Status);
    }

    // 6. Complete workflow
    [Fact]
    public async Task CompleteWorkflow_ShouldMarkAsCompleted()
    {
        var startDecision = await _engine.StartWorkflow("taxi-request", "1.0.0", null);

        var decision = await _engine.CompleteWorkflow(startDecision.InstanceId);

        Assert.Equal(WorkflowInstanceStatus.Completed, decision.Status);
        Assert.Null(decision.NextStep);

        var state = _stateStore.GetState(startDecision.InstanceId);
        Assert.Equal(WorkflowInstanceStatus.Completed, state.Status);
    }

    // 7. Terminate workflow
    [Fact]
    public async Task TerminateWorkflow_ShouldMarkAsCancelled()
    {
        var startDecision = await _engine.StartWorkflow("taxi-request", "1.0.0", null);

        var decision = await _engine.TerminateWorkflow(startDecision.InstanceId);

        Assert.Equal(WorkflowInstanceStatus.Cancelled, decision.Status);
        Assert.Null(decision.NextStep);

        var state = _stateStore.GetState(startDecision.InstanceId);
        Assert.Equal(WorkflowInstanceStatus.Cancelled, state.Status);
    }
}
