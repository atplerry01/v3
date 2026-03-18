namespace Whycespace.Domain.Identity.Events;

public sealed record ParticipantSuspendedEvent(
    Guid EventId,
    Guid ParticipantId,
    DateTimeOffset Timestamp
)
{
    public static ParticipantSuspendedEvent Create(Guid participantId)
        => new(Guid.NewGuid(), participantId, DateTimeOffset.UtcNow);
}
