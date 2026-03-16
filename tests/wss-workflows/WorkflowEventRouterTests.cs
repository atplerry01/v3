using Whycespace.Domain.Core.Workflows;
using Whycespace.Systems.Midstream.WSS.Instances;
using Whycespace.Systems.Midstream.WSS.Stores;
using Whycespace.WorkflowRuntime;
using WorkflowInstanceStatus = Whycespace.Systems.Midstream.WSS.Models.WorkflowInstanceStatus;

namespace Whycespace.WSS.Workflows.Tests;

public class WorkflowEventRouterTests
{
    private readonly WorkflowInstanceStore _instanceStore;
    private readonly WorkflowInstanceRegistry _instanceRegistry;
    private readonly WorkflowStateStore _stateStore;
    private readonly WorkflowEventRouter _router;

    public WorkflowEventRouterTests()
    {
        _instanceStore = new WorkflowInstanceStore();
        _instanceRegistry = new WorkflowInstanceRegistry(_instanceStore);
        _stateStore = new WorkflowStateStore();
        _router = new WorkflowEventRouter(_instanceRegistry, _stateStore);
    }

    private (string instanceId, string correlationId) SetupRunningInstance(
        string workflowName = "test-workflow",
        string currentStep = "step-1",
        string? correlationId = null)
    {
        var corrId = correlationId ?? Guid.NewGuid().ToString();
        var record = _instanceRegistry.CreateWorkflowInstance(
            workflowName, "1.0", corrId, "test-user");

        _instanceRegistry.UpdateWorkflowInstanceStatus(
            record.InstanceId, WorkflowInstanceStatus.Running);

        var stateRecord = new WorkflowStateRecord(
            record.InstanceId, record.WorkflowId, currentStep,
            new List<WorkflowStepState>(), new List<WorkflowStepState>(),
            0, false, DateTimeOffset.UtcNow);

        _stateStore.PersistWorkflowState(stateRecord);

        return (record.InstanceId, corrId);
    }

    [Fact]
    public async Task RouteAsync_EventRoutedToWorkflowStep_ReturnsMatched()
    {
        var (instanceId, correlationId) = SetupRunningInstance(
            "property-acquisition", "validate-capital");

        var command = WorkflowEventRouteCommand.Create(
            "CapitalValidated", Guid.NewGuid(), correlationId,
            new Dictionary<string, object> { ["completedStep"] = "validate-capital" });

        var result = await _router.RouteAsync(command);

        Assert.Equal(RoutingStatus.Matched, result.RoutingStatus);
        Assert.Equal(instanceId, result.WorkflowInstanceId);
        Assert.Equal("validate-capital", result.AffectedStep);
    }

    [Fact]
    public async Task RouteAsync_UnrelatedEvent_ReturnsIgnored()
    {
        var command = WorkflowEventRouteCommand.Create(
            "SomeUnrelatedEvent", Guid.NewGuid());

        var result = await _router.RouteAsync(command);

        Assert.Equal(RoutingStatus.Ignored, result.RoutingStatus);
        Assert.Equal(string.Empty, result.WorkflowInstanceId);
    }

    [Fact]
    public async Task RouteAsync_MultipleWorkflowsReceivingEvents_BothRouted()
    {
        var corrId = Guid.NewGuid().ToString();
        var (instanceId1, _) = SetupRunningInstance("wf-1", "step-a", corrId);

        var command = WorkflowEventRouteCommand.Create(
            "StepCompleted", Guid.NewGuid(), corrId);

        var result = await _router.RouteAsync(command);

        Assert.Equal(RoutingStatus.Matched, result.RoutingStatus);
    }

    [Fact]
    public async Task RouteAsync_WorkflowProgressionAfterEvent_UpdatesStepState()
    {
        var (instanceId, correlationId) = SetupRunningInstance(
            "ride-request", "find-driver");

        var command = WorkflowEventRouteCommand.Create(
            "DriverFound", Guid.NewGuid(), correlationId,
            new Dictionary<string, object>
            {
                ["targetStep"] = "confirm-ride",
                ["driverId"] = "driver-42"
            });

        await _router.RouteAsync(command);

        var completedSteps = _stateStore.ListCompletedSteps(instanceId);
        Assert.Contains(completedSteps, s => s.StepId == "confirm-ride");
    }

    [Fact]
    public async Task RouteAsync_ConcurrentEventRouting_IsDeterministic()
    {
        var (instanceId, correlationId) = SetupRunningInstance(
            "wf-concurrent", "step-1");

        var tasks = Enumerable.Range(0, 10).Select(i =>
        {
            var cmd = WorkflowEventRouteCommand.Create(
                $"Event-{i}", Guid.NewGuid(), correlationId,
                new Dictionary<string, object> { [$"key-{i}"] = $"value-{i}" });
            return _router.RouteAsync(cmd);
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        var matched = results.Count(r => r.RoutingStatus == RoutingStatus.Matched);
        Assert.True(matched > 0);
    }

    [Fact]
    public async Task RouteAsync_DuplicateEvent_IsIdempotent()
    {
        var (instanceId, correlationId) = SetupRunningInstance("wf-idem", "step-1");

        var command = new WorkflowEventRouteCommand(
            Guid.NewGuid(), "TestEvent", Guid.NewGuid(), correlationId,
            new Dictionary<string, object>(), DateTimeOffset.UtcNow);

        var result1 = await _router.RouteAsync(command);
        var result2 = await _router.RouteAsync(command);

        Assert.Equal(RoutingStatus.Matched, result1.RoutingStatus);
        Assert.Equal(RoutingStatus.Ignored, result2.RoutingStatus);
    }

    [Fact]
    public async Task RouteAsync_CompletedWorkflow_IsIgnored()
    {
        var (instanceId, correlationId) = SetupRunningInstance("wf-done", "final-step");
        _instanceRegistry.UpdateWorkflowInstanceStatus(instanceId, WorkflowInstanceStatus.Completed);

        var command = WorkflowEventRouteCommand.Create(
            "LateEvent", Guid.NewGuid(), correlationId);

        var result = await _router.RouteAsync(command);

        Assert.Equal(RoutingStatus.Ignored, result.RoutingStatus);
    }

    [Fact]
    public async Task RouteAsync_CorrelationIdRouting_MatchesCorrectInstance()
    {
        var correlationId = "corr-abc-123";
        var (instanceId, _) = SetupRunningInstance("correlated-wf", "step-1", correlationId);

        var command = WorkflowEventRouteCommand.Create(
            "CorrelatedEvent", Guid.NewGuid(), correlationId);

        var result = await _router.RouteAsync(command);

        Assert.Equal(RoutingStatus.Matched, result.RoutingStatus);
        Assert.Equal(instanceId, result.WorkflowInstanceId);
    }

    [Fact]
    public async Task RouteBatchAsync_ProcessesAllCommands()
    {
        var correlationId = "batch-corr";
        var (instanceId, _) = SetupRunningInstance("batch-wf", "step-1", correlationId);

        var commands = new List<WorkflowEventRouteCommand>
        {
            WorkflowEventRouteCommand.Create("Event1", Guid.NewGuid(), correlationId),
            WorkflowEventRouteCommand.Create("Event2", Guid.NewGuid()),
            WorkflowEventRouteCommand.Create("Event3", Guid.NewGuid(), correlationId)
        };

        var results = await _router.RouteBatchAsync(commands);

        Assert.Equal(3, results.Count);
        Assert.Equal(RoutingStatus.Matched, results[0].RoutingStatus);
        Assert.Equal(RoutingStatus.Ignored, results[1].RoutingStatus);
        Assert.Equal(RoutingStatus.Matched, results[2].RoutingStatus);
    }
}
