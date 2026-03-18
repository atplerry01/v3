namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceDisputeEscalatedEvent(
    Guid EventId,
    Guid DisputeId,
    string EscalationReason,
    DateTime EscalatedAt);
