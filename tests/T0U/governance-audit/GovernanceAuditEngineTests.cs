using Whycespace.Engines.T3I.Reporting.Governance.Engines;
using Whycespace.Engines.T3I.Reporting.Governance.Models;
using Whycespace.Engines.T3I.Shared;

namespace Whycespace.GovernanceAudit.Tests;

public class GovernanceAuditEngineTests
{
    private readonly GovernanceAuditEngine _engine = new();

    [Fact]
    public void Execute_ValidCommand_ReturnsSuccess()
    {
        var command = CreateValidCommand();

        var result = _engine.Execute(IntelligenceContext<GenerateGovernanceAuditCommand>.Create(command));

        Assert.True(result.Success);
        Assert.NotEmpty(result.Output!.AuditId);
        Assert.NotEmpty(result.Output!.AuditHash);
        Assert.Equal(command.ProposalId, result.Output!.ProposalId);
        Assert.Equal(command.ActionType, result.Output!.ActionType);
    }

    [Fact]
    public void Execute_EmptyProposalId_ReturnsFailure()
    {
        var command = CreateValidCommand() with { ProposalId = Guid.Empty };

        var result = _engine.Execute(IntelligenceContext<GenerateGovernanceAuditCommand>.Create(command));

        Assert.False(result.Success);
        Assert.Contains("ProposalId", result.Error);
    }

    [Fact]
    public void Execute_EmptyPerformedBy_ReturnsFailure()
    {
        var command = CreateValidCommand() with { PerformedBy = Guid.Empty };

        var result = _engine.Execute(IntelligenceContext<GenerateGovernanceAuditCommand>.Create(command));

        Assert.False(result.Success);
        Assert.Contains("PerformedBy", result.Error);
    }

    [Fact]
    public void Execute_DefaultTimestamp_ReturnsFailure()
    {
        var command = CreateValidCommand() with { Timestamp = default };

        var result = _engine.Execute(IntelligenceContext<GenerateGovernanceAuditCommand>.Create(command));

        Assert.False(result.Success);
        Assert.Contains("Timestamp", result.Error);
    }

    [Fact]
    public void Execute_InvalidActionType_ReturnsFailure()
    {
        var command = CreateValidCommand() with { ActionType = (GovernanceAuditActionType)999 };

        var result = _engine.Execute(IntelligenceContext<GenerateGovernanceAuditCommand>.Create(command));

        Assert.False(result.Success);
        Assert.Contains("action type", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(GovernanceAuditActionType.ProposalCreated)]
    [InlineData(GovernanceAuditActionType.VoteCast)]
    [InlineData(GovernanceAuditActionType.DecisionApproved)]
    [InlineData(GovernanceAuditActionType.DisputeRaised)]
    [InlineData(GovernanceAuditActionType.EmergencyTriggered)]
    [InlineData(GovernanceAuditActionType.EvidenceRecorded)]
    public void Execute_AllActionTypes_ReturnsSuccess(GovernanceAuditActionType actionType)
    {
        var command = CreateValidCommand() with { ActionType = actionType };

        var result = _engine.Execute(IntelligenceContext<GenerateGovernanceAuditCommand>.Create(command));

        Assert.True(result.Success);
        Assert.Equal(actionType, result.Output!.ActionType);
    }

    [Fact]
    public void GenerateAuditRecord_ReturnsCompleteRecord()
    {
        var command = CreateValidCommand();

        var record = _engine.GenerateAuditRecord(command);

        Assert.NotEmpty(record.AuditId);
        Assert.Equal(command.ProposalId, record.ProposalId);
        Assert.Equal(command.ActionType, record.ActionType);
        Assert.Equal(command.PerformedBy, record.PerformedBy);
        Assert.Equal(command.ActionReferenceId, record.ActionReferenceId);
        Assert.Equal(command.ActionDescription, record.ActionDescription);
        Assert.NotEmpty(record.AuditHash);
        Assert.Equal(command.Timestamp, record.RecordedAt);
    }

    [Fact]
    public void Execute_ConcurrentCommands_ProduceDeterministicOutput()
    {
        var command = CreateValidCommand();

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => _engine.Execute(IntelligenceContext<GenerateGovernanceAuditCommand>.Create(command))))
            .ToArray();

        Task.WaitAll(tasks);

        var results = tasks.Select(t => t.Result).ToList();
        var distinctHashes = results.Select(r => r.Output!.AuditHash).Distinct().ToList();
        var distinctIds = results.Select(r => r.Output!.AuditId).Distinct().ToList();

        Assert.Single(distinctHashes);
        Assert.Single(distinctIds);
    }

    private static GenerateGovernanceAuditCommand CreateValidCommand() =>
        new(
            CommandId: Guid.NewGuid(),
            ProposalId: Guid.NewGuid(),
            ActionType: GovernanceAuditActionType.ProposalCreated,
            PerformedBy: Guid.NewGuid(),
            ActionReferenceId: Guid.NewGuid(),
            ActionDescription: "Test governance action",
            Timestamp: new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc));
}
