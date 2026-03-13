namespace Whycespace.EventSchema.Models;

public sealed class EventSchema
{
    public string EventType { get; }

    public int SchemaVersion { get; }

    public IReadOnlyDictionary<string, string> PayloadStructure { get; }

    public DateTime CreatedAt { get; }

    public EventSchema(
        string eventType,
        int version,
        IReadOnlyDictionary<string, string> payload)
    {
        EventType = eventType;
        SchemaVersion = version;
        PayloadStructure = payload;
        CreatedAt = DateTime.UtcNow;
    }
}
