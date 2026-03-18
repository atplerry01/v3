namespace Whycespace.Domain.Events.Governance;

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
