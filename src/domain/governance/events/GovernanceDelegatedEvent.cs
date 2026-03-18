namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceDelegatedEvent(
    Guid EventId,
    string DelegationId,
    string DelegatorGuardianId,
    string DelegateGuardianId,
    string DelegatedRole,
    string AuthorityDomain,
    DateTime DelegationStart,
    DateTime DelegationEnd,
    DateTime Timestamp);
