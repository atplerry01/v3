namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceDecisionEscalatedEvent(
    Guid EventId,
    string ProposalId,
    string EscalationReason,
    DateTime EscalatedAt);
