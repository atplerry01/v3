namespace Whycespace.Domain.Identity;

public sealed record ParticipantProfile(
    ParticipantId ParticipantId,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Country,
    DateTimeOffset CreatedAt
);
