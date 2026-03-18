namespace Whycespace.Systems.Upstream.Governance.Events;

public sealed record GovernanceEmergencyTriggeredEvent(
    Guid EventId,
    string EmergencyId,
    string TriggeredBy,
    string Severity,
    string Reason,
    DateTimeOffset Timestamp);
