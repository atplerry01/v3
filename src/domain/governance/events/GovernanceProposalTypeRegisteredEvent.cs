namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceProposalTypeRegisteredEvent(
    Guid EventId,
    string ProposalType,
    string Description,
    Guid RegisteredByGuardianId,
    DateTime RegisteredAt,
    int EventVersion);
