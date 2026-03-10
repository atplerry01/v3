namespace Whycespace.EventIdempotency.Models;

public sealed class ProcessedEvent
{
    public Guid EventId { get; }

    public string EventType { get; }

    public string Topic { get; }

    public string PartitionKey { get; }

    public DateTime ProcessedAt { get; }

    public ProcessedEvent(
        Guid eventId,
        string eventType,
        string topic,
        string partitionKey)
    {
        EventId = eventId;
        EventType = eventType;
        Topic = topic;
        PartitionKey = partitionKey;
        ProcessedAt = DateTime.UtcNow;
    }
}
