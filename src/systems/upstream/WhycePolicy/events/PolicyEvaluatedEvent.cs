namespace Whycespace.Systems.Upstream.WhycePolicy.Events;

public sealed record PolicyEvaluatedEvent(
    Guid EventId,
    string PolicyId,
    Guid ActorId,
    string Domain,
    bool Allowed,
    string Reason,
    DateTimeOffset Timestamp);
