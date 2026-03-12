namespace Whycespace.Domain.Core.Participants;

public sealed record ParticipantProfile(
    ParticipantId ParticipantId,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Country,
    DateTimeOffset CreatedAt
);
