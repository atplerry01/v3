namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceDomainScopeDeactivatedEvent(
    Guid EventId,
    string AuthorityDomain,
    string Reason,
    Guid DeactivatedByGuardianId,
    DateTime DeactivatedAt);
