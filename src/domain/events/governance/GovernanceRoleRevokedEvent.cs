namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceRoleRevokedEvent(
    Guid EventId,
    Guid GuardianId,
    string RevokedRole,
    Guid RevokedBy,
    string Reason,
    DateTime Timestamp);
