namespace Whycespace.Engines.T3I.Governance.Results;

using Whycespace.Engines.T3I.Governance.Commands;

public sealed record GovernanceAuditEngineResult(
    bool Success,
    string AuditId,
    Guid ProposalId,
    GovernanceAuditActionType ActionType,
    string AuditHash,
    string Message,
    DateTime ExecutedAt);
