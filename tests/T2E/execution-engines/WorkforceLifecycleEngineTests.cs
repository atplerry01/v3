namespace Whycespace.Tests.ExecutionEngines;

using Whycespace.Engines.T2E.Workforce.Engines;
using Whycespace.Engines.T2E.Workforce.Models;
using Whycespace.Domain.Core.Workforce;
using Whycespace.Domain.Core.Operators;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class WorkforceLifecycleEngineTests
{
    private readonly WorkforceLifecycleEngine _engine = new();

    [Fact]
    public void ValidActivation_FromRegistered_ReturnsAccepted()
    {
        var workforce = CreateWorkforce(WorkerStatus.Registered);
        var operatorAgg = CreateOperator();
        var command = CreateCommand(LifecycleAction.Activate);

        var decision = WorkforceLifecycleEngine.ProcessLifecycle(workforce, operatorAgg, command);

        Assert.True(decision.Success);
        Assert.Equal("Registered", decision.PreviousStatus);
        Assert.Equal("Active", decision.NewStatus);
    }

    [Fact]
    public void ValidSuspension_FromActive_ReturnsAccepted()
    {
        var workforce = CreateWorkforce(WorkerStatus.Active);
        var operatorAgg = CreateOperator("heos.workforce.suspend");
        var command = CreateCommand(LifecycleAction.Suspend);

        var decision = WorkforceLifecycleEngine.ProcessLifecycle(workforce, operatorAgg, command);

        Assert.True(decision.Success);
        Assert.Equal("Active", decision.PreviousStatus);
        Assert.Equal("Suspended", decision.NewStatus);
    }

    [Fact]
    public void ValidTermination_FromActive_ReturnsAccepted()
    {
        var workforce = CreateWorkforce(WorkerStatus.Active);
        var operatorAgg = CreateOperator("heos.workforce.terminate");
        var command = CreateCommand(LifecycleAction.Terminate);

        var decision = WorkforceLifecycleEngine.ProcessLifecycle(workforce, operatorAgg, command);

        Assert.True(decision.Success);
        Assert.Equal("Active", decision.PreviousStatus);
        Assert.Equal("Terminated", decision.NewStatus);
    }

    [Fact]
    public void ValidSetUnavailable_FromActive_ReturnsAccepted()
    {
        var workforce = CreateWorkforce(WorkerStatus.Active);
        var operatorAgg = CreateOperator();
        var command = CreateCommand(LifecycleAction.SetUnavailable);

        var decision = WorkforceLifecycleEngine.ProcessLifecycle(workforce, operatorAgg, command);

        Assert.True(decision.Success);
        Assert.Equal("Active", decision.PreviousStatus);
        Assert.Equal("Unavailable", decision.NewStatus);
    }

    [Fact]
    public void ReactivationSuccess_FromSuspended_ReturnsAccepted()
    {
        var workforce = CreateWorkforce(WorkerStatus.Suspended);
        var operatorAgg = CreateOperator("heos.workforce.reactivate");
        var command = CreateCommand(LifecycleAction.Reactivate);

        var decision = WorkforceLifecycleEngine.ProcessLifecycle(workforce, operatorAgg, command);

        Assert.True(decision.Success);
        Assert.Equal("Suspended", decision.PreviousStatus);
        Assert.Equal("Active", decision.NewStatus);
    }

    [Fact]
    public void ActivateFromUnavailable_ReturnsAccepted()
    {
        var workforce = CreateWorkforce(WorkerStatus.Unavailable);
        var operatorAgg = CreateOperator();
        var command = CreateCommand(LifecycleAction.Activate);

        var decision = WorkforceLifecycleEngine.ProcessLifecycle(workforce, operatorAgg, command);

        Assert.True(decision.Success);
        Assert.Equal("Unavailable", decision.PreviousStatus);
        Assert.Equal("Active", decision.NewStatus);
    }

    [Fact]
    public void InvalidTransition_TerminatedToActive_ReturnsRejected()
    {
        var workforce = CreateWorkforce(WorkerStatus.Terminated);
        var operatorAgg = CreateOperator();
        var command = CreateCommand(LifecycleAction.Activate);

        var decision = WorkforceLifecycleEngine.ProcessLifecycle(workforce, operatorAgg, command);

        Assert.False(decision.Success);
        Assert.Contains("Invalid lifecycle transition", decision.Reason);
    }

    [Fact]
    public void ReactivationRejection_AfterTermination_ReturnsRejected()
    {
        var workforce = CreateWorkforce(WorkerStatus.Terminated);
        var operatorAgg = CreateOperator("heos.workforce.reactivate");
        var command = CreateCommand(LifecycleAction.Reactivate);

        var decision = WorkforceLifecycleEngine.ProcessLifecycle(workforce, operatorAgg, command);

        Assert.False(decision.Success);
        Assert.Contains("Invalid lifecycle transition", decision.Reason);
    }

    [Fact]
    public void SuspendWithoutAuthority_ReturnsRejected()
    {
        var workforce = CreateWorkforce(WorkerStatus.Active);
        var operatorAgg = CreateOperator(); // no suspend scope
        var command = CreateCommand(LifecycleAction.Suspend);

        var decision = WorkforceLifecycleEngine.ProcessLifecycle(workforce, operatorAgg, command);

        Assert.False(decision.Success);
        Assert.Contains("authority", decision.Reason);
    }

    [Fact]
    public void TerminateWithoutElevatedAuthority_ReturnsRejected()
    {
        var workforce = CreateWorkforce(WorkerStatus.Active);
        var operatorAgg = CreateOperator("heos.workforce.suspend"); // wrong scope
        var command = CreateCommand(LifecycleAction.Terminate);

        var decision = WorkforceLifecycleEngine.ProcessLifecycle(workforce, operatorAgg, command);

        Assert.False(decision.Success);
        Assert.Contains("authority", decision.Reason);
    }

    [Fact]
    public void InactiveOperator_ReturnsRejected()
    {
        var workforce = CreateWorkforce(WorkerStatus.Active);
        var operatorAgg = CreateOperator("heos.workforce.suspend");
        operatorAgg.Suspend();
        var command = CreateCommand(LifecycleAction.Suspend);

        var decision = WorkforceLifecycleEngine.ProcessLifecycle(workforce, operatorAgg, command);

        Assert.False(decision.Success);
        Assert.Contains("Operator is not active", decision.Reason);
    }

    [Fact]
    public async Task ExecuteAsync_ValidActivation_ReturnsSuccess()
    {
        var context = CreateContext(
            workerStatus: "Registered",
            lifecycleAction: "Activate");

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("WorkforceLifecycleTransitioned", result.Events[0].EventType);
    }

    [Fact]
    public async Task ExecuteAsync_MissingFields_ReturnsFailure()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Lifecycle",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidTransition_ReturnsFailure()
    {
        var context = CreateContext(
            workerStatus: "Terminated",
            lifecycleAction: "Activate");

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    private static WorkforceAggregate CreateWorkforce(WorkerStatus status)
    {
        var workforce = WorkforceAggregate.Register(
            WorkerId.New(), "TestWorker", new[] { "General" });

        if (status != WorkerStatus.Active)
            workforce.SetStatus(status);

        return workforce;
    }

    private static OperatorAggregate CreateOperator(params string[] scopes)
    {
        return OperatorAggregate.Register(
            OperatorId.New(), "TestOperator", scopes);
    }

    private static WorkforceLifecycleCommand CreateCommand(LifecycleAction action)
    {
        return new WorkforceLifecycleCommand(
            Guid.NewGuid(),
            action,
            Guid.NewGuid(),
            "Test reason",
            DateTimeOffset.UtcNow);
    }

    private static EngineContext CreateContext(
        string workerStatus = "Active",
        string lifecycleAction = "Activate",
        string? operatorStatus = "Active",
        string[]? operatorScopes = null)
    {
        var workerId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();

        var scopes = operatorScopes ?? lifecycleAction switch
        {
            "Suspend" => new[] { "heos.workforce.suspend" },
            "Terminate" => new[] { "heos.workforce.terminate" },
            "Reactivate" => new[] { "heos.workforce.reactivate" },
            _ => Array.Empty<string>()
        };

        var data = new Dictionary<string, object>
        {
            ["workforceId"] = workerId.ToString(),
            ["lifecycleAction"] = lifecycleAction,
            ["requestedByOperatorId"] = operatorId.ToString(),
            ["reason"] = "Test lifecycle transition",
            ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
            ["workerName"] = "TestWorker",
            ["workerStatus"] = workerStatus,
            ["workerCapabilities"] = new[] { "General" },
            ["operatorName"] = "TestOperator",
            ["operatorScopes"] = scopes,
            ["operatorStatus"] = operatorStatus ?? "Active"
        };

        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Lifecycle",
            "partition-1", data);
    }
}
