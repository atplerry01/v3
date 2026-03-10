namespace Whycespace.Contracts.Events;

public interface IEvent
{
    Guid EventId { get; }
    string EventType { get; }
    Guid AggregateId { get; }
    DateTimeOffset Timestamp { get; }
}
