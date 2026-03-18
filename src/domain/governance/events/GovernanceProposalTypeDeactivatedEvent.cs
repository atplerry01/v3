namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceProposalTypeDeactivatedEvent(
    Guid EventId,
    string ProposalType,
    string Reason,
    Guid DeactivatedByGuardianId,
    DateTime DeactivatedAt,
    int EventVersion);
