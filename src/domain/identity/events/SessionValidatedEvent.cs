namespace Whycespace.Domain.Events.Core.Identity;

public sealed record SessionValidatedEvent(
    Guid EventId,
    Guid SessionId,
    Guid IdentityId,
    bool Valid,
    DateTime ValidatedAt,
    int EventVersion);
