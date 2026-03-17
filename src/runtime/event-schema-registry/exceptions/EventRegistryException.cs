namespace Whycespace.Runtime.EventSchemaRegistry.Exceptions;

public sealed class EventRegistryException : Exception
{
    public string EventId { get; }

    public IReadOnlyList<string> ValidationErrors { get; }

    public EventRegistryException(string eventId, string message)
        : base(message)
    {
        EventId = eventId;
        ValidationErrors = [];
    }

    public EventRegistryException(string eventId, IReadOnlyList<string> validationErrors)
        : base($"Event '{eventId}' failed validation: {string.Join("; ", validationErrors)}")
    {
        EventId = eventId;
        ValidationErrors = validationErrors;
    }

    public EventRegistryException(string eventId, string message, Exception innerException)
        : base(message, innerException)
    {
        EventId = eventId;
        ValidationErrors = [];
    }
}
