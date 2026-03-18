namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceDelegationExpiredEvent(
    Guid EventId,
    string DelegationId,
    string DelegatorGuardianId,
    string DelegateGuardianId,
    DateTime ExpiredAt);
