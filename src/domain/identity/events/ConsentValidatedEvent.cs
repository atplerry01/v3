namespace Whycespace.Domain.Identity.Events;

public sealed record ConsentValidatedEvent(
    Guid EventId,
    Guid IdentityId,
    string ConsentType,
    bool Valid,
    DateTime ValidatedAt,
    int EventVersion);
