namespace Whycespace.Systems.Upstream.WhycePolicy.Events;

public sealed record PolicyLifecycleChangedEvent(
    Guid EventId,
    string PolicyId,
    string PreviousState,
    string NewState,
    string ChangedBy,
    DateTimeOffset Timestamp);
