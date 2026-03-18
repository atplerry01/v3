namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceDomainScopeDeactivatedEvent(
    Guid EventId,
    string AuthorityDomain,
    string Reason,
    Guid DeactivatedByGuardianId,
    DateTime DeactivatedAt);
