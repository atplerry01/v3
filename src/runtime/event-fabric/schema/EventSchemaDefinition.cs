namespace Whycespace.EventFabric.Schema;

public sealed class EventSchemaDefinition
{
    public string EventType { get; }

    public int Version { get; }

    public IReadOnlyDictionary<string, string> Fields { get; }

    public DateTime RegisteredAt { get; }

    public EventSchemaDefinition(
        string eventType,
        int version,
        IReadOnlyDictionary<string, string> fields)
    {
        EventType = eventType;
        Version = version;
        Fields = fields;
        RegisteredAt = DateTime.UtcNow;
    }
}
