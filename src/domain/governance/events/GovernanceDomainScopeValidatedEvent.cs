namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceDomainScopeValidatedEvent(
    Guid EventId,
    Guid ProposalId,
    string AuthorityDomain,
    string ProposalType,
    Guid ValidatedByGuardianId,
    DateTime ValidatedAt);
