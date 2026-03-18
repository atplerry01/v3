namespace Whycespace.Domain.Identity.Events;

public sealed record SessionValidatedEvent(
    Guid EventId,
    Guid SessionId,
    Guid IdentityId,
    bool Valid,
    DateTime ValidatedAt,
    int EventVersion);
