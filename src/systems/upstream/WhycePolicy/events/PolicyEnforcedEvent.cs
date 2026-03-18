namespace Whycespace.Systems.Upstream.WhycePolicy.Events;

public sealed record PolicyEnforcedEvent(
    Guid EventId,
    string PolicyId,
    string Domain,
    string Operation,
    bool Allowed,
    string Action,
    DateTimeOffset Timestamp);
