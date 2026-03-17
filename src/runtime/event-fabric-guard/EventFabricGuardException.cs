namespace Whycespace.Runtime.EventFabricGuard;

public sealed class EventFabricGuardException : Exception
{
    public string? EventIdentifier { get; }

    public EventFabricGuardException(string message, string? eventIdentifier = null)
        : base(message)
    {
        EventIdentifier = eventIdentifier;
    }

    public EventFabricGuardException(string message, string? eventIdentifier, Exception innerException)
        : base(message, innerException)
    {
        EventIdentifier = eventIdentifier;
    }
}
