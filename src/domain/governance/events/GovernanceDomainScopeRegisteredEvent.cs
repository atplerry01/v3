namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceDomainScopeRegisteredEvent(
    Guid EventId,
    string AuthorityDomain,
    string Description,
    Guid RegisteredByGuardianId,
    DateTime RegisteredAt);
