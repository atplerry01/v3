namespace Whycespace.Systems.Downstream.Cwg.Participants;

public sealed record CWGParticipantRecord(
    Guid ParticipantId,
    Guid IdentityId,
    string Name,
    string Role,
    string ClusterId,
    string Status,
    DateTimeOffset JoinedAt,
    decimal TrustScore = 0m,
    string? Description = null
);
