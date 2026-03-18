namespace Whycespace.Runtime.Persistence.EventStore;

public sealed record EventAppendResult(
    bool Success,
    Guid EventId,
    long NewVersion,
    string? ErrorMessage = null
)
{
    public static EventAppendResult Ok(Guid eventId, long version) => new(true, eventId, version);
    public static EventAppendResult Failed(Guid eventId, string error) => new(false, eventId, -1, error);
}
