namespace Whycespace.Engines.T3I.Reporting.Governance;

using Whycespace.Engines.T3I.Reporting.Governance.Commands;
using Whycespace.Engines.T3I.Reporting.Governance.Results;

public sealed class GovernanceAuditEngine
{
    public GovernanceAuditEngineResult Execute(GenerateGovernanceAuditCommand command)
    {
        var validationError = Validate(command);
        if (validationError is not null)
        {
            return new GovernanceAuditEngineResult(
                Success: false,
                AuditId: string.Empty,
                ProposalId: command.ProposalId,
                ActionType: command.ActionType,
                AuditHash: string.Empty,
                Message: validationError,
                ExecutedAt: DateTime.UtcNow);
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

        return new GovernanceAuditEngineResult(
            Success: true,
            AuditId: auditId,
            ProposalId: command.ProposalId,
            ActionType: command.ActionType,
            AuditHash: auditHash,
            Message: $"Governance audit record generated for {command.ActionType}",
            ExecutedAt: DateTime.UtcNow);
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
