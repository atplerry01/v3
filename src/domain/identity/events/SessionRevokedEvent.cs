namespace Whycespace.Domain.Identity.Events;

public sealed record SessionRevokedEvent(
    Guid EventId,
    Guid SessionId,
    Guid IdentityId,
    DateTime RevokedAt,
    int EventVersion);
