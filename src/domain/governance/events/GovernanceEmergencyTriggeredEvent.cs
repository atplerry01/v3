namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceEmergencyTriggeredEvent(
    Guid EventId,
    string EmergencyActionId,
    string EmergencyType,
    string TargetDomain,
    string TriggeredByGuardianId,
    string Reason,
    DateTime TriggeredAt);
