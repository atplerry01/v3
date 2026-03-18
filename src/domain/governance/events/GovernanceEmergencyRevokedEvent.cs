namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceEmergencyRevokedEvent(
    Guid EventId,
    string EmergencyActionId,
    string RevokedByGuardianId,
    string Reason,
    DateTime RevokedAt);
