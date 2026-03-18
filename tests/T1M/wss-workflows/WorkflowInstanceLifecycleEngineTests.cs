namespace Whycespace.Tests.WssWorkflows;

using Whycespace.Contracts.Engines;
using Whycespace.Shared.Primitives.Common;
using Whycespace.Engines.T1M.WSS.Lifecycle;
using Xunit;

public sealed class WorkflowInstanceLifecycleEngineTests
{
    private readonly WorkflowInstanceLifecycleEngine _engine = new();

    private static EngineContext CreateContext(Dictionary<string, object> data)
    {
        return new EngineContext(
            Guid.NewGuid(),
            "wf-lifecycle-test",
            "lifecycle-step",
            new PartitionKey("test-partition"),
            data);
    }

    // --- Valid transitions ---

    [Fact]
    public async Task ExecuteAsync_CreatedToRunning_ReturnsSuccess()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["workflowInstanceId"] = "wf-instance-001",
            ["currentStatus"] = "Created",
            ["requestedTransition"] = "Start"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("WorkflowLifecycleTransitionAccepted", result.Events[0].EventType);
        Assert.Equal("Running", result.Output["newStatus"]);
        Assert.Equal(true, result.Output["transitionAccepted"]);
    }

    [Fact]
    public async Task ExecuteAsync_RunningToCompleted_ReturnsSuccess()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["workflowInstanceId"] = "wf-instance-002",
            ["currentStatus"] = "Running",
            ["requestedTransition"] = "Complete"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Completed", result.Output["newStatus"]);
    }

    [Fact]
    public async Task ExecuteAsync_RunningToFailed_ReturnsSuccess()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["workflowInstanceId"] = "wf-instance-003",
            ["currentStatus"] = "Running",
            ["requestedTransition"] = "Fail"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Failed", result.Output["newStatus"]);
    }

    [Fact]
    public async Task ExecuteAsync_FailedToRetrying_ReturnsSuccess()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["workflowInstanceId"] = "wf-instance-004",
            ["currentStatus"] = "Failed",
            ["requestedTransition"] = "Recover"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Retrying", result.Output["newStatus"]);
    }

    [Fact]
    public async Task ExecuteAsync_RetryingToRunning_ReturnsSuccess()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["workflowInstanceId"] = "wf-instance-005",
            ["currentStatus"] = "Retrying",
            ["requestedTransition"] = "Start"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Running", result.Output["newStatus"]);
    }

    [Fact]
    public async Task ExecuteAsync_RunningToTerminated_ReturnsSuccess()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["workflowInstanceId"] = "wf-instance-006",
            ["currentStatus"] = "Running",
            ["requestedTransition"] = "Terminate"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Terminated", result.Output["newStatus"]);
    }

    [Fact]
    public async Task ExecuteAsync_FailedToTerminated_ReturnsSuccess()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["workflowInstanceId"] = "wf-instance-007",
            ["currentStatus"] = "Failed",
            ["requestedTransition"] = "Terminate"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Terminated", result.Output["newStatus"]);
    }

    [Fact]
    public async Task ExecuteAsync_WaitingToRunning_ReturnsSuccess()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["workflowInstanceId"] = "wf-instance-008",
            ["currentStatus"] = "Waiting",
            ["requestedTransition"] = "Start"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Running", result.Output["newStatus"]);
    }

    // --- Invalid transitions ---

    [Fact]
    public async Task ExecuteAsync_CompletedToRunning_RejectsTransition()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["workflowInstanceId"] = "wf-instance-invalid-001",
            ["currentStatus"] = "Completed",
            ["requestedTransition"] = "Start"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("WorkflowLifecycleTransitionRejected", result.Events[0].EventType);
        Assert.Equal(false, result.Output["transitionAccepted"]);
    }

    [Fact]
    public async Task ExecuteAsync_TerminatedToRunning_RejectsTransition()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["workflowInstanceId"] = "wf-instance-invalid-002",
            ["currentStatus"] = "Terminated",
            ["requestedTransition"] = "Start"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("not allowed", result.Output["reason"] as string);
    }

    [Fact]
    public async Task ExecuteAsync_CreatedToComplete_RejectsTransition()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["workflowInstanceId"] = "wf-instance-invalid-003",
            ["currentStatus"] = "Created",
            ["requestedTransition"] = "Complete"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyInstanceId_RejectsTransition()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["workflowInstanceId"] = "",
            ["currentStatus"] = "Created",
            ["requestedTransition"] = "Start"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("must not be empty", result.Output["reason"] as string);
    }

    // --- Determinism ---

    [Fact]
    public void EvaluateTransition_SameInput_ProducesSameResult()
    {
        var timestamp = DateTimeOffset.Parse("2026-03-15T12:00:00Z");
        var command = new WorkflowLifecycleCommand(
            "wf-deterministic",
            WorkflowLifecycleStatus.Created,
            WorkflowLifecycleTransition.Start,
            timestamp);

        var result1 = _engine.EvaluateTransition(command);
        var result2 = _engine.EvaluateTransition(command);

        Assert.Equal(result1.TransitionAccepted, result2.TransitionAccepted);
        Assert.Equal(result1.PreviousStatus, result2.PreviousStatus);
        Assert.Equal(result1.NewStatus, result2.NewStatus);
        Assert.Equal(result1.TransitionReason, result2.TransitionReason);
        Assert.Equal(result1.EvaluatedAt, result2.EvaluatedAt);
    }

    // --- Concurrency safety ---

    [Fact]
    public async Task EvaluateTransition_ConcurrentCalls_AllSucceed()
    {
        var timestamp = DateTimeOffset.Parse("2026-03-15T12:00:00Z");
        var command = new WorkflowLifecycleCommand(
            "wf-concurrent",
            WorkflowLifecycleStatus.Running,
            WorkflowLifecycleTransition.Complete,
            timestamp);

        var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
            _engine.EvaluateTransition(command))).ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r =>
        {
            Assert.True(r.TransitionAccepted);
            Assert.Equal(WorkflowLifecycleStatus.Completed, r.NewStatus);
        });
    }

    // --- Engine name ---

    [Fact]
    public void Name_ReturnsWorkflowInstanceLifecycle()
    {
        Assert.Equal("WorkflowInstanceLifecycle", _engine.Name);
    }

    // --- Typed result structure ---

    [Fact]
    public void EvaluateTransition_AcceptedResult_HasCorrectStructure()
    {
        var timestamp = DateTimeOffset.Parse("2026-03-15T10:00:00Z");
        var command = new WorkflowLifecycleCommand(
            "wf-structure-test",
            WorkflowLifecycleStatus.Failed,
            WorkflowLifecycleTransition.Recover,
            timestamp);

        var result = _engine.EvaluateTransition(command);

        Assert.True(result.TransitionAccepted);
        Assert.Equal("wf-structure-test", result.WorkflowInstanceId);
        Assert.Equal(WorkflowLifecycleStatus.Failed, result.PreviousStatus);
        Assert.Equal(WorkflowLifecycleStatus.Retrying, result.NewStatus);
        Assert.Equal(timestamp, result.EvaluatedAt);
        Assert.Contains("Recover", result.TransitionReason);
    }

    [Fact]
    public void EvaluateTransition_RejectedResult_KeepsCurrentStatus()
    {
        var timestamp = DateTimeOffset.Parse("2026-03-15T10:00:00Z");
        var command = new WorkflowLifecycleCommand(
            "wf-reject-test",
            WorkflowLifecycleStatus.Completed,
            WorkflowLifecycleTransition.Fail,
            timestamp);

        var result = _engine.EvaluateTransition(command);

        Assert.False(result.TransitionAccepted);
        Assert.Equal(WorkflowLifecycleStatus.Completed, result.PreviousStatus);
        Assert.Equal(WorkflowLifecycleStatus.Completed, result.NewStatus);
    }
}
