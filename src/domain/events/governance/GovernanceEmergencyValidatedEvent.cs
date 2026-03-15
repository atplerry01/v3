namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceEmergencyValidatedEvent(
    Guid EventId,
    string EmergencyActionId,
    string GuardianId,
    DateTime ValidatedAt);
