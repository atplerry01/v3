namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceEmergencyRevokedEvent(
    Guid EventId,
    string EmergencyActionId,
    string RevokedByGuardianId,
    string Reason,
    DateTime RevokedAt);
