namespace Whycespace.Systems.Upstream.WhycePolicy.Events;

public sealed record PolicyConflictDetectedEvent(
    Guid EventId,
    string PolicyIdA,
    string PolicyIdB,
    string ConflictType,
    string Resolution,
    DateTimeOffset Timestamp);
