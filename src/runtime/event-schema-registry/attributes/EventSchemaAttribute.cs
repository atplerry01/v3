namespace Whycespace.Runtime.EventSchemaRegistry.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class EventSchemaAttribute : Attribute
{
    public string EventId { get; }

    public string Domain { get; }

    public int Version { get; }

    public string? Description { get; set; }

    public EventSchemaAttribute(string eventId, string domain, int version = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(domain);
        ArgumentOutOfRangeException.ThrowIfLessThan(version, 1);

        EventId = eventId;
        Domain = domain;
        Version = version;
    }
}
