namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceDisputeEscalatedEvent(
    Guid EventId,
    Guid DisputeId,
    string EscalationReason,
    DateTime EscalatedAt);
