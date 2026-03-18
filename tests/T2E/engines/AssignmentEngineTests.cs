namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T2E.Workforce.Engines;
using Whycespace.Engines.T2E.Workforce.Models;
using Whycespace.Domain.Clusters.Operations.Shared;
using Whycespace.Domain.Identity;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class AssignmentEngineTests
{
    private readonly AssignmentEngine _engine = new();

    [Fact]
    public async Task SuccessfulAssignment_ReturnsAssigned()
    {
        var workerId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();
        var scope = "cluster-mobility";

        var context = CreateContext(workerId, taskId, operatorId, scope,
            taskType: "TaxiRide",
            workerCapabilities: new[] { "TaxiRide", "CustomerSupport" },
            operatorScopes: new[] { "cluster-mobility" });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("WorkerAssigned", result.Events[0].EventType);
        Assert.Equal(workerId, result.Events[0].AggregateId);
    }

    [Fact]
    public async Task WorkerUnavailable_ReturnsRejection()
    {
        var workerId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();
        var busyTaskId = Guid.NewGuid();

        var context = CreateContext(workerId, taskId, operatorId, "scope-1",
            taskType: "TaxiRide",
            workerCapabilities: new[] { "TaxiRide" },
            operatorScopes: new[] { "scope-1" },
            workerAvailability: "Busy",
            workerCurrentTaskId: busyTaskId.ToString());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CapabilityMismatch_ReturnsRejection()
    {
        var workerId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();

        var context = CreateContext(workerId, taskId, operatorId, "scope-1",
            taskType: "PropertyMaintenance",
            workerCapabilities: new[] { "TaxiRide", "CustomerSupport" },
            operatorScopes: new[] { "scope-1" });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task OperatorAuthorizationFailure_ReturnsRejection()
    {
        var workerId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();

        var context = CreateContext(workerId, taskId, operatorId, "cluster-property",
            taskType: "TaxiRide",
            workerCapabilities: new[] { "TaxiRide" },
            operatorScopes: new[] { "cluster-mobility" });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task DuplicateAssignment_ReturnsRejection()
    {
        var workerId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();

        var context = CreateContext(workerId, taskId, operatorId, "scope-1",
            taskType: "TaxiRide",
            workerCapabilities: new[] { "TaxiRide" },
            operatorScopes: new[] { "scope-1" },
            workerAvailability: "Busy",
            workerCurrentTaskId: taskId.ToString());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SuspendedWorker_ReturnsRejection()
    {
        var workerId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();

        var context = CreateContext(workerId, taskId, operatorId, "scope-1",
            taskType: "TaxiRide",
            workerCapabilities: new[] { "TaxiRide" },
            operatorScopes: new[] { "scope-1" },
            workerStatus: "Suspended");

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SuspendedOperator_ReturnsRejection()
    {
        var workerId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();

        var context = CreateContext(workerId, taskId, operatorId, "scope-1",
            taskType: "TaxiRide",
            workerCapabilities: new[] { "TaxiRide" },
            operatorScopes: new[] { "scope-1" },
            operatorStatus: "Suspended");

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingRequiredFields_ReturnsFailure()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Assign",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public void AssignWorker_Directly_SuccessfulAssignment()
    {
        var workforce = WorkforceAggregate.Register(
            WorkerId.New(), "John", new[] { "TaxiRide", "Inspection" });

        var operatorAgg = OperatorAggregate.Register(
            OperatorId.New(), "OperatorA", new[] { "cluster-mobility" });

        var command = new AssignmentCommand(
            workforce.WorkerId, Guid.NewGuid(), "TaxiRide",
            operatorAgg.OperatorId, "cluster-mobility");

        var decision = AssignmentEngine.AssignWorker(workforce, operatorAgg, command);

        Assert.True(decision.Assigned);
        Assert.Equal("Worker assigned successfully", decision.Reason);
    }

    [Fact]
    public void AssignWorker_Directly_CapabilityMismatch()
    {
        var workforce = WorkforceAggregate.Register(
            WorkerId.New(), "John", new[] { "TaxiRide" });

        var operatorAgg = OperatorAggregate.Register(
            OperatorId.New(), "OperatorA", new[] { "cluster-property" });

        var command = new AssignmentCommand(
            workforce.WorkerId, Guid.NewGuid(), "PropertyMaintenance",
            operatorAgg.OperatorId, "cluster-property");

        var decision = AssignmentEngine.AssignWorker(workforce, operatorAgg, command);

        Assert.False(decision.Assigned);
        Assert.Contains("capability", decision.Reason);
    }

    private static EngineContext CreateContext(
        Guid workerId, Guid taskId, Guid operatorId, string scope,
        string taskType = "TaxiRide",
        string[]? workerCapabilities = null,
        string[]? operatorScopes = null,
        string workerStatus = "Active",
        string workerAvailability = "Available",
        string? workerCurrentTaskId = null,
        string operatorStatus = "Active")
    {
        var data = new Dictionary<string, object>
        {
            ["workforceId"] = workerId.ToString(),
            ["taskId"] = taskId.ToString(),
            ["taskType"] = taskType,
            ["requestedByOperatorId"] = operatorId.ToString(),
            ["assignmentScope"] = scope,
            ["workerName"] = "TestWorker",
            ["workerCapabilities"] = workerCapabilities ?? new[] { "TaxiRide" },
            ["workerStatus"] = workerStatus,
            ["workerAvailability"] = workerAvailability,
            ["operatorName"] = "TestOperator",
            ["operatorScopes"] = operatorScopes ?? new[] { scope },
            ["operatorStatus"] = operatorStatus
        };

        if (workerCurrentTaskId is not null)
            data["workerCurrentTaskId"] = workerCurrentTaskId;

        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Assign",
            "partition-1", data);
    }
}
