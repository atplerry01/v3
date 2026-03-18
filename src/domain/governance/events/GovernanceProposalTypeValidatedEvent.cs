namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceProposalTypeValidatedEvent(
    Guid EventId,
    string ProposalType,
    string AuthorityDomain,
    Guid ValidatedByGuardianId,
    DateTime ValidatedAt,
    int EventVersion);
