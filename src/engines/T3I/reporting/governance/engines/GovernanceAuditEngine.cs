namespace Whycespace.Engines.T3I.Reporting.Governance.Engines;

using Whycespace.Engines.T3I.Reporting.Governance.Models;
using Whycespace.Engines.T3I.Shared;

public sealed class GovernanceAuditEngine : IIntelligenceEngine<GenerateGovernanceAuditCommand, GovernanceAuditEngineResult>
{
    public string EngineName => "GovernanceAudit";

    public IntelligenceResult<GovernanceAuditEngineResult> Execute(IntelligenceContext<GenerateGovernanceAuditCommand> context)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var command = context.Input;

        var validationError = Validate(command);
        if (validationError is not null)
        {
            var failResult = new GovernanceAuditEngineResult(
                Success: false,
                AuditId: string.Empty,
                ProposalId: command.ProposalId,
                ActionType: command.ActionType,
                AuditHash: string.Empty,
                Message: validationError,
                ExecutedAt: DateTime.UtcNow);
            return IntelligenceResult<GovernanceAuditEngineResult>.Fail(validationError, IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));
        }

        var auditId = GovernanceAuditHashGenerator.GenerateAuditId(
            command.ProposalId,
            command.ActionType,
            command.PerformedBy,
            command.Timestamp);

        var auditHash = GovernanceAuditHashGenerator.GenerateAuditHash(
            command.ProposalId,
            command.ActionType,
            command.PerformedBy,
            command.ActionReferenceId,
            command.Timestamp);

        var result = new GovernanceAuditEngineResult(
            Success: true,
            AuditId: auditId,
            ProposalId: command.ProposalId,
            ActionType: command.ActionType,
            AuditHash: auditHash,
            Message: $"Governance audit record generated for {command.ActionType}",
            ExecutedAt: DateTime.UtcNow);
        return IntelligenceResult<GovernanceAuditEngineResult>.Ok(result, IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));
    }

    public GovernanceAuditRecord GenerateAuditRecord(GenerateGovernanceAuditCommand command)
    {
        var auditId = GovernanceAuditHashGenerator.GenerateAuditId(
            command.ProposalId,
            command.ActionType,
            command.PerformedBy,
            command.Timestamp);

        var auditHash = GovernanceAuditHashGenerator.GenerateAuditHash(
            command.ProposalId,
            command.ActionType,
            command.PerformedBy,
            command.ActionReferenceId,
            command.Timestamp);

        return new GovernanceAuditRecord(
            AuditId: auditId,
            ProposalId: command.ProposalId,
            ActionType: command.ActionType,
            PerformedBy: command.PerformedBy,
            ActionReferenceId: command.ActionReferenceId,
            ActionDescription: command.ActionDescription,
            AuditHash: auditHash,
            RecordedAt: command.Timestamp);
    }

    private static string? Validate(GenerateGovernanceAuditCommand command)
    {
        if (command.ProposalId == Guid.Empty)
            return "ProposalId must not be empty";

        if (!Enum.IsDefined(command.ActionType))
            return $"Invalid action type: {command.ActionType}";

        if (command.PerformedBy == Guid.Empty)
            return "PerformedBy must be a valid guardian or operator identifier";

        if (command.Timestamp == default)
            return "Timestamp must be a valid date";

        return null;
    }
}
