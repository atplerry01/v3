namespace Whycespace.Domain.Core.Participants.Events;

public sealed record ParticipantRoleAssignedEvent(
    Guid EventId,
    Guid ParticipantId,
    ParticipantRole Role,
    DateTimeOffset Timestamp
)
{
    public static ParticipantRoleAssignedEvent Create(Guid participantId, ParticipantRole role)
        => new(Guid.NewGuid(), participantId, role, DateTimeOffset.UtcNow);
}
