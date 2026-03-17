namespace Whycespace.Domain.Events.Core.Participants;

using Whycespace.Domain.Core.Participants;

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
