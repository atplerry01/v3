namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceEmergencyValidatedEvent(
    Guid EventId,
    string EmergencyActionId,
    string GuardianId,
    DateTime ValidatedAt);
