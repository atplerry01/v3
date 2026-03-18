namespace Whycespace.DecisionEngines.Tests;

using Whycespace.Engines.T3I.Reporting.Workforce;
using Whycespace.Domain.Core.Workforce;
using Whycespace.Contracts.Engines;

public sealed class WorkforceAuditEngineTests
{
    private readonly WorkforceAuditEngine _engine = new();

    private static readonly DateTimeOffset FixedTimestamp = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AuditRecordGeneration_ReturnsValidRecord()
    {
        var workforce = WorkforceAggregate.Register(
            WorkerId.New(), "Alice", new[] { "TaxiRide" });

        var command = new WorkforceAuditCommand(
            workforce.WorkerId,
            AuditActionType.WorkforceRegistered,
            Guid.NewGuid(),
            Guid.NewGuid(),
            FixedTimestamp,
            "Initial registration");

        var record = WorkforceAuditEngine.GenerateAuditRecord(workforce, command);

        Assert.NotEqual(Guid.Empty, record.AuditId);
        Assert.Equal(command.WorkforceId, record.WorkforceId);
        Assert.Equal(AuditActionType.WorkforceRegistered, record.ActionType);
        Assert.Equal(command.ActionReferenceId, record.ActionReferenceId);
        Assert.Equal(command.PerformedBy, record.PerformedBy);
        Assert.Equal(FixedTimestamp, record.Timestamp);
        Assert.Contains("Alice", record.AuditSummary);
        Assert.Contains("registered", record.AuditSummary);
    }

    [Fact]
    public void DeterministicAuditOutput_SameInputsProduceSameId()
    {
        var workforce = WorkforceAggregate.Register(
            WorkerId.New(), "Bob", new[] { "Inspection" });

        var refId = Guid.NewGuid();
        var performedBy = Guid.NewGuid();

        var command = new WorkforceAuditCommand(
            workforce.WorkerId,
            AuditActionType.LifecycleChanged,
            refId,
            performedBy,
            FixedTimestamp,
            "Status changed to Active");

        var record1 = WorkforceAuditEngine.GenerateAuditRecord(workforce, command);
        var record2 = WorkforceAuditEngine.GenerateAuditRecord(workforce, command);

        Assert.Equal(record1.AuditId, record2.AuditId);
        Assert.Equal(record1.AuditSummary, record2.AuditSummary);
    }

    [Fact]
    public async Task ExecuteAsync_ValidCommand_ReturnsSuccess()
    {
        var context = CreateContext(
            actionType: "LifecycleChanged",
            details: "Activated from Registered");

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("WorkforceAuditRecordCreated", result.Events[0].EventType);
        Assert.NotNull(result.Output["auditId"]);
        Assert.NotNull(result.Output["auditSummary"]);
    }

    [Fact]
    public async Task ExecuteAsync_MissingWorkforceId_ReturnsFailure()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Audit",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownActionType_ReturnsFailure()
    {
        var context = CreateContext(actionType: "NonExistentAction");

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidTimestamp_ReturnsFailure()
    {
        var workerId = Guid.NewGuid();
        var data = new Dictionary<string, object>
        {
            ["workforceId"] = workerId.ToString(),
            ["actionType"] = "LifecycleChanged",
            ["actionReferenceId"] = Guid.NewGuid().ToString(),
            ["performedBy"] = Guid.NewGuid().ToString(),
            ["timestamp"] = "not-a-date",
            ["details"] = "Test",
            ["workerName"] = "TestWorker",
            ["workerCapabilities"] = new[] { "General" }
        };

        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Audit",
            "partition-1", data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public void AllActionTypes_ProduceValidSummaries()
    {
        var workforce = WorkforceAggregate.Register(
            WorkerId.New(), "Charlie", new[] { "General" });

        foreach (var actionType in Enum.GetValues<AuditActionType>())
        {
            var command = new WorkforceAuditCommand(
                workforce.WorkerId,
                actionType,
                Guid.NewGuid(),
                Guid.NewGuid(),
                FixedTimestamp,
                $"Details for {actionType}");

            var record = WorkforceAuditEngine.GenerateAuditRecord(workforce, command);

            Assert.Contains("Charlie", record.AuditSummary);
            Assert.Contains($"Details for {actionType}", record.AuditSummary);
        }
    }

    [Fact]
    public async Task ExecuteAsync_MissingActionReferenceId_ReturnsFailure()
    {
        var data = new Dictionary<string, object>
        {
            ["workforceId"] = Guid.NewGuid().ToString(),
            ["actionType"] = "LifecycleChanged",
            ["performedBy"] = Guid.NewGuid().ToString(),
            ["timestamp"] = FixedTimestamp.ToString("O"),
            ["details"] = "Test",
            ["workerName"] = "TestWorker",
            ["workerCapabilities"] = new[] { "General" }
        };

        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Audit",
            "partition-1", data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    private static EngineContext CreateContext(
        string actionType = "WorkforceRegistered",
        string details = "Test audit entry")
    {
        var workerId = Guid.NewGuid();

        var data = new Dictionary<string, object>
        {
            ["workforceId"] = workerId.ToString(),
            ["actionType"] = actionType,
            ["actionReferenceId"] = Guid.NewGuid().ToString(),
            ["performedBy"] = Guid.NewGuid().ToString(),
            ["timestamp"] = FixedTimestamp.ToString("O"),
            ["details"] = details,
            ["workerName"] = "TestWorker",
            ["workerCapabilities"] = new[] { "General" }
        };

        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Audit",
            "partition-1", data);
    }
}
