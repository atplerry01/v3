namespace Whycespace.Runtime.EventSchemaRegistry.Models;

public sealed record EventDescriptor(
    string EventId,
    string Domain,
    EventSchemaVersion Version,
    Type EventType,
    string? Description,
    IReadOnlyList<EventPropertyDescriptor> Properties
);

public sealed record EventPropertyDescriptor(
    string Name,
    string TypeName,
    bool IsRequired
);
