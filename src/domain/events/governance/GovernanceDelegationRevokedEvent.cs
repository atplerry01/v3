namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceDelegationRevokedEvent(
    Guid EventId,
    string DelegationId,
    string DelegatorGuardianId,
    string DelegateGuardianId,
    string Reason,
    string RevokedBy,
    DateTime Timestamp);
