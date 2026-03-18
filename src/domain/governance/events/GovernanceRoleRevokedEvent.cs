namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceRoleRevokedEvent(
    Guid EventId,
    Guid GuardianId,
    string RevokedRole,
    Guid RevokedBy,
    string Reason,
    DateTime Timestamp);
