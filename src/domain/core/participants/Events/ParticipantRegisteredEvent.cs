namespace Whycespace.Domain.Events.Core.Participants;

public sealed record ParticipantRegisteredEvent(
    Guid EventId,
    Guid ParticipantId,
    string Email,
    DateTimeOffset Timestamp
)
{
    public static ParticipantRegisteredEvent Create(Guid participantId, string email)
        => new(Guid.NewGuid(), participantId, email, DateTimeOffset.UtcNow);
}
