namespace Whycespace.Engines.T3I.Reporting.Governance.Models;


public sealed record GovernanceAuditEngineResult(
    bool Success,
    string AuditId,
    Guid ProposalId,
    GovernanceAuditActionType ActionType,
    string AuditHash,
    string Message,
    DateTime ExecutedAt);
