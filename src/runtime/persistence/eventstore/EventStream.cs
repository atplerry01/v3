namespace Whycespace.Runtime.Persistence.EventStore;

public sealed record EventStream(
    Guid AggregateId,
    string AggregateType,
    IReadOnlyList<EventStreamEntry> Events,
    long CurrentVersion
);

public sealed record EventStreamEntry(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    long Version,
    DateTimeOffset Timestamp,
    string Payload
);
