namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceProposalTypeRegisteredEvent(
    Guid EventId,
    string ProposalType,
    string Description,
    Guid RegisteredByGuardianId,
    DateTime RegisteredAt,
    int EventVersion);
