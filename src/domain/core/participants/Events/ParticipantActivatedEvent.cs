namespace Whycespace.Domain.Core.Participants.Events;

public sealed record ParticipantActivatedEvent(
    Guid EventId,
    Guid ParticipantId,
    DateTimeOffset Timestamp
)
{
    public static ParticipantActivatedEvent Create(Guid participantId)
        => new(Guid.NewGuid(), participantId, DateTimeOffset.UtcNow);
}
