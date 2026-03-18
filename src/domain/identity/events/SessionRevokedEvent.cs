namespace Whycespace.Domain.Events.Core.Identity;

public sealed record SessionRevokedEvent(
    Guid EventId,
    Guid SessionId,
    Guid IdentityId,
    DateTime RevokedAt,
    int EventVersion);
