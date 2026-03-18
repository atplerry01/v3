namespace Whycespace.Systems.Upstream.WhycePolicy.Events;

public sealed record PolicyRegisteredEvent(
    Guid EventId,
    string PolicyId,
    string PolicyName,
    string Domain,
    string Priority,
    DateTimeOffset Timestamp);
