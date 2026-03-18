namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceDelegationExpiredEvent(
    Guid EventId,
    string DelegationId,
    string DelegatorGuardianId,
    string DelegateGuardianId,
    DateTime ExpiredAt);
