namespace Whycespace.Domain.Events.Core.Identity;

public sealed record ConsentValidatedEvent(
    Guid EventId,
    Guid IdentityId,
    string ConsentType,
    bool Valid,
    DateTime ValidatedAt,
    int EventVersion);
