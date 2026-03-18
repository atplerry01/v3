namespace Whycespace.Contracts.Events;

public abstract record EventBase(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    DateTimeOffset Timestamp
) : IEvent;
