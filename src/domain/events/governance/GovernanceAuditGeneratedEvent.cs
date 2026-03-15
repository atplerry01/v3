namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceAuditGeneratedEvent(
    Guid EventId,
    string AuditId,
    Guid ProposalId,
    string ActionType,
    Guid PerformedBy,
    string AuditHash,
    DateTime RecordedAt);
