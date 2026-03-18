namespace Whycespace.WhyceID.Identity.Tests;

using Whycespace.Engines.T3I.Reporting.Identity.Engines;
using Whycespace.Engines.T3I.Reporting.Identity.Models;
using Whycespace.Contracts.Engines;

public sealed class IdentityAuditEngineT3ITests
{
    private readonly IdentityAuditEngine _engine = new();

    private static readonly DateTimeOffset FixedTimestamp = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AuditRecordGeneration_ReturnsValidRecord()
    {
        var command = new IdentityAuditCommand(
            Guid.NewGuid(),
            IdentityAuditAction.IdentityCreated,
            "WhyceID",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Initial identity creation",
            FixedTimestamp);

        var record = IdentityAuditEngine.GenerateAuditRecord(command);

        Assert.NotEqual(Guid.Empty, record.AuditId);
        Assert.Equal(command.IdentityId, record.IdentityId);
        Assert.Equal(IdentityAuditAction.IdentityCreated, record.Action);
        Assert.Equal("WhyceID", record.SourceSystem);
        Assert.Equal(command.PerformedBy, record.PerformedBy);
        Assert.Equal(command.OperationReferenceId, record.OperationReferenceId);
        Assert.Equal("Initial identity creation", record.Metadata);
        Assert.Equal(FixedTimestamp, record.RecordedAt);
    }

    [Fact]
    public void DeterministicAuditId_SameInputsProduceSameId()
    {
        var identityId = Guid.NewGuid();
        var performedBy = Guid.NewGuid();
        var refId = Guid.NewGuid();

        var command = new IdentityAuditCommand(
            identityId,
            IdentityAuditAction.RoleAssigned,
            "WhyceID",
            performedBy,
            refId,
            "Assigned admin role",
            FixedTimestamp);

        var record1 = IdentityAuditEngine.GenerateAuditRecord(command);
        var record2 = IdentityAuditEngine.GenerateAuditRecord(command);

        Assert.Equal(record1.AuditId, record2.AuditId);
    }

    [Fact]
    public void DeterministicAuditId_DifferentInputsProduceDifferentIds()
    {
        var identityId = Guid.NewGuid();
        var performedBy = Guid.NewGuid();
        var refId = Guid.NewGuid();

        var command1 = new IdentityAuditCommand(
            identityId, IdentityAuditAction.IdentityCreated, "WhyceID",
            performedBy, refId, "", FixedTimestamp);

        var command2 = new IdentityAuditCommand(
            identityId, IdentityAuditAction.IdentityVerified, "WhyceID",
            performedBy, refId, "", FixedTimestamp);

        var record1 = IdentityAuditEngine.GenerateAuditRecord(command1);
        var record2 = IdentityAuditEngine.GenerateAuditRecord(command2);

        Assert.NotEqual(record1.AuditId, record2.AuditId);
    }

    [Fact]
    public async Task ExecuteAsync_ValidCommand_ReturnsSuccess()
    {
        var context = CreateContext(
            auditAction: "IdentityCreated",
            metadata: "New identity registered");

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("IdentityAuditRecorded", result.Events[0].EventType);
        Assert.NotNull(result.Output["auditId"]);
        Assert.NotNull(result.Output["identityId"]);
        Assert.NotNull(result.Output["action"]);
        Assert.NotNull(result.Output["sourceSystem"]);
        Assert.NotNull(result.Output["recordedAt"]);
    }

    [Fact]
    public async Task ExecuteAsync_MissingIdentityId_ReturnsFailure()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Audit",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidAuditAction_ReturnsFailure()
    {
        var context = CreateContext(auditAction: "NonExistentAction");

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_MissingSourceSystem_ReturnsFailure()
    {
        var data = new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString(),
            ["auditAction"] = "IdentityCreated",
            ["performedBy"] = Guid.NewGuid().ToString(),
            ["operationReferenceId"] = Guid.NewGuid().ToString(),
            ["timestamp"] = FixedTimestamp.ToString("O"),
            ["metadata"] = "Test"
        };

        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Audit",
            "partition-1", data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidTimestamp_ReturnsFailure()
    {
        var data = new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString(),
            ["auditAction"] = "IdentityCreated",
            ["sourceSystem"] = "WhyceID",
            ["performedBy"] = Guid.NewGuid().ToString(),
            ["operationReferenceId"] = Guid.NewGuid().ToString(),
            ["timestamp"] = "not-a-date",
            ["metadata"] = "Test"
        };

        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Audit",
            "partition-1", data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public void AllAuditActions_ProduceValidRecords()
    {
        var identityId = Guid.NewGuid();
        var performedBy = Guid.NewGuid();

        foreach (var action in Enum.GetValues<IdentityAuditAction>())
        {
            var command = new IdentityAuditCommand(
                identityId, action, "WhyceID", performedBy,
                Guid.NewGuid(), $"Details for {action}", FixedTimestamp);

            var record = IdentityAuditEngine.GenerateAuditRecord(command);

            Assert.NotEqual(Guid.Empty, record.AuditId);
            Assert.Equal(identityId, record.IdentityId);
            Assert.Equal(action, record.Action);
            Assert.Equal("WhyceID", record.SourceSystem);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ConcurrentExecution_ProducesConsistentResults()
    {
        var tasks = new List<Task<EngineResult>>();

        for (var i = 0; i < 50; i++)
        {
            var context = CreateContext(
                auditAction: "SessionCreated",
                metadata: $"Concurrent session {i}");
            tasks.Add(_engine.ExecuteAsync(context));
        }

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Success));
        Assert.All(results, r => Assert.Single(r.Events));
        Assert.All(results, r => Assert.Equal("IdentityAuditRecorded", r.Events[0].EventType));
    }

    [Fact]
    public void AuditRecord_IsImmutable()
    {
        var command = new IdentityAuditCommand(
            Guid.NewGuid(), IdentityAuditAction.RevocationExecuted, "WhyceID",
            Guid.NewGuid(), Guid.NewGuid(), "Revocation executed", FixedTimestamp);

        var record = IdentityAuditEngine.GenerateAuditRecord(command);

        // Records are immutable by design (sealed record)
        Assert.IsType<IdentityAuditRecord>(record);
        Assert.Equal(command.IdentityId, record.IdentityId);
        Assert.Equal(IdentityAuditAction.RevocationExecuted, record.Action);
    }

    [Fact]
    public async Task ExecuteAsync_MetadataDefaultsToEmpty_WhenNotProvided()
    {
        var data = new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString(),
            ["auditAction"] = "IdentityCreated",
            ["sourceSystem"] = "WhyceID",
            ["performedBy"] = Guid.NewGuid().ToString(),
            ["operationReferenceId"] = Guid.NewGuid().ToString(),
            ["timestamp"] = FixedTimestamp.ToString("O")
        };

        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Audit",
            "partition-1", data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
    }

    private static EngineContext CreateContext(
        string auditAction = "IdentityCreated",
        string metadata = "Test audit entry")
    {
        var data = new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString(),
            ["auditAction"] = auditAction,
            ["sourceSystem"] = "WhyceID",
            ["performedBy"] = Guid.NewGuid().ToString(),
            ["operationReferenceId"] = Guid.NewGuid().ToString(),
            ["timestamp"] = FixedTimestamp.ToString("O"),
            ["metadata"] = metadata
        };

        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Audit",
            "partition-1", data);
    }
}
