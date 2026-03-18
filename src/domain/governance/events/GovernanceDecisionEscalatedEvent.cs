namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceDecisionEscalatedEvent(
    Guid EventId,
    string ProposalId,
    string EscalationReason,
    DateTime EscalatedAt);
