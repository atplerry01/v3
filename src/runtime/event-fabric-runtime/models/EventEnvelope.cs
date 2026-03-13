namespace Whycespace.EventFabricRuntime.Models;

public sealed class EventEnvelope
{
    public string EventId { get; }

    public string EventType { get; }

    public object Payload { get; }

    public DateTime TimestampUtc { get; }

    public EventEnvelope(string eventId, string eventType, object payload)
    {
        EventId = eventId;
        EventType = eventType;
        Payload = payload;
        TimestampUtc = DateTime.UtcNow;
    }
}
