namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T2E.Workforce.Engines;
using Whycespace.Engines.T2E.Workforce.Models;
using Whycespace.Domain.Core.Workforce;
using Whycespace.Domain.Core.Operators;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class WorkforceSchedulingEngineTests
{
    private readonly WorkforceSchedulingEngine _engine = new();

    private static readonly DateTimeOffset BaseStart = new(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset BaseEnd = new(2026, 4, 1, 17, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SuccessfulScheduling_ReturnsScheduled()
    {
        var context = CreateContext(
            taskType: "PropertyMaintenance",
            workerCapabilities: new[] { "PropertyMaintenance" },
            operatorScopes: new[] { "cluster-property" },
            scheduleScope: "cluster-property");

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("WorkforceScheduled", result.Events[0].EventType);
    }

    [Fact]
    public async Task InvalidTimeWindow_EndBeforeStart_ReturnsRejection()
    {
        var context = CreateContext(
            taskType: "Inspection",
            workerCapabilities: new[] { "Inspection" },
            operatorScopes: new[] { "scope-1" },
            scheduleScope: "scope-1",
            scheduleStart: BaseEnd,
            scheduleEnd: BaseStart);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task InvalidTimeWindow_EqualStartEnd_ReturnsRejection()
    {
        var context = CreateContext(
            taskType: "Inspection",
            workerCapabilities: new[] { "Inspection" },
            operatorScopes: new[] { "scope-1" },
            scheduleScope: "scope-1",
            scheduleStart: BaseStart,
            scheduleEnd: BaseStart);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task OverlappingSchedule_ReturnsRejection()
    {
        var existingSchedules = new[]
        {
            new ScheduleRecord(Guid.NewGuid(), BaseStart, BaseEnd)
        };

        var context = CreateContext(
            taskType: "Inspection",
            workerCapabilities: new[] { "Inspection" },
            operatorScopes: new[] { "scope-1" },
            scheduleScope: "scope-1",
            scheduleStart: BaseStart.AddHours(2),
            scheduleEnd: BaseEnd.AddHours(2),
            workerSchedules: existingSchedules);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task NonOverlappingSchedule_Succeeds()
    {
        var existingSchedules = new[]
        {
            new ScheduleRecord(Guid.NewGuid(), BaseStart, BaseEnd)
        };

        var context = CreateContext(
            taskType: "Inspection",
            workerCapabilities: new[] { "Inspection" },
            operatorScopes: new[] { "scope-1" },
            scheduleScope: "scope-1",
            scheduleStart: BaseEnd,
            scheduleEnd: BaseEnd.AddHours(4),
            workerSchedules: existingSchedules);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task WorkerUnavailable_ReturnsRejection()
    {
        var context = CreateContext(
            taskType: "TaxiRide",
            workerCapabilities: new[] { "TaxiRide" },
            operatorScopes: new[] { "scope-1" },
            scheduleScope: "scope-1",
            workerAvailability: "Busy",
            workerCurrentTaskId: Guid.NewGuid().ToString());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task OperatorAuthorityFailure_ReturnsRejection()
    {
        var context = CreateContext(
            taskType: "TaxiRide",
            workerCapabilities: new[] { "TaxiRide" },
            operatorScopes: new[] { "cluster-mobility" },
            scheduleScope: "cluster-property");

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SuspendedWorker_ReturnsRejection()
    {
        var context = CreateContext(
            taskType: "TaxiRide",
            workerCapabilities: new[] { "TaxiRide" },
            operatorScopes: new[] { "scope-1" },
            scheduleScope: "scope-1",
            workerStatus: "Suspended");

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CapabilityMismatch_ReturnsRejection()
    {
        var context = CreateContext(
            taskType: "PropertyMaintenance",
            workerCapabilities: new[] { "TaxiRide" },
            operatorScopes: new[] { "scope-1" },
            scheduleScope: "scope-1");

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingRequiredFields_ReturnsFailure()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Schedule",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public void ScheduleWorkforce_Directly_SuccessfulScheduling()
    {
        var workforce = WorkforceAggregate.Register(
            WorkerId.New(), "Jane", new[] { "Inspection" });

        var operatorAgg = OperatorAggregate.Register(
            OperatorId.New(), "OperatorB", new[] { "cluster-property" });

        var command = new WorkforceScheduleCommand(
            workforce.WorkerId, operatorAgg.OperatorId, Guid.NewGuid(),
            "Inspection", BaseStart, BaseEnd, "cluster-property");

        var decision = WorkforceSchedulingEngine.ScheduleWorkforce(workforce, operatorAgg, command);

        Assert.True(decision.Scheduled);
        Assert.Equal("Workforce scheduled successfully", decision.Reason);
        Assert.Equal(BaseStart, decision.ScheduleStart);
        Assert.Equal(BaseEnd, decision.ScheduleEnd);
    }

    [Fact]
    public void ScheduleWorkforce_Directly_OverlappingSchedule()
    {
        var workforce = WorkforceAggregate.Register(
            WorkerId.New(), "Jane", new[] { "Inspection" });
        workforce.AddSchedule(new ScheduleRecord(Guid.NewGuid(), BaseStart, BaseEnd));

        var operatorAgg = OperatorAggregate.Register(
            OperatorId.New(), "OperatorB", new[] { "cluster-property" });

        var command = new WorkforceScheduleCommand(
            workforce.WorkerId, operatorAgg.OperatorId, Guid.NewGuid(),
            "Inspection", BaseStart.AddHours(4), BaseEnd, "cluster-property");

        var decision = WorkforceSchedulingEngine.ScheduleWorkforce(workforce, operatorAgg, command);

        Assert.False(decision.Scheduled);
        Assert.Contains("overlaps", decision.Reason);
    }

    private static EngineContext CreateContext(
        string taskType = "TaxiRide",
        string[]? workerCapabilities = null,
        string[]? operatorScopes = null,
        string scheduleScope = "scope-1",
        DateTimeOffset? scheduleStart = null,
        DateTimeOffset? scheduleEnd = null,
        string workerStatus = "Active",
        string workerAvailability = "Available",
        string? workerCurrentTaskId = null,
        string operatorStatus = "Active",
        ScheduleRecord[]? workerSchedules = null)
    {
        var workerId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var data = new Dictionary<string, object>
        {
            ["workforceId"] = workerId.ToString(),
            ["operatorId"] = operatorId.ToString(),
            ["taskId"] = taskId.ToString(),
            ["taskType"] = taskType,
            ["scheduleScope"] = scheduleScope,
            ["scheduleStart"] = (scheduleStart ?? BaseStart).ToString("O"),
            ["scheduleEnd"] = (scheduleEnd ?? BaseEnd).ToString("O"),
            ["workerName"] = "TestWorker",
            ["workerCapabilities"] = workerCapabilities ?? new[] { taskType },
            ["workerStatus"] = workerStatus,
            ["workerAvailability"] = workerAvailability,
            ["operatorName"] = "TestOperator",
            ["operatorScopes"] = operatorScopes ?? new[] { scheduleScope },
            ["operatorStatus"] = operatorStatus,
            ["workerSchedules"] = workerSchedules ?? Array.Empty<ScheduleRecord>()
        };

        if (workerCurrentTaskId is not null)
            data["workerCurrentTaskId"] = workerCurrentTaskId;

        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Schedule",
            "partition-1", data);
    }
}
