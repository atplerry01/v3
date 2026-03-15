namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceDomainScopeRegisteredEvent(
    Guid EventId,
    string AuthorityDomain,
    string Description,
    Guid RegisteredByGuardianId,
    DateTime RegisteredAt);
