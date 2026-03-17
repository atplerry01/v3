namespace Whycespace.Engines.T3I.Reporting.Governance;

using Whycespace.Engines.T3I.Reporting.Governance.Commands;

public sealed record GovernanceAuditRecord(
    string AuditId,
    Guid ProposalId,
    GovernanceAuditActionType ActionType,
    Guid PerformedBy,
    Guid ActionReferenceId,
    string ActionDescription,
    string AuditHash,
    DateTime RecordedAt);
