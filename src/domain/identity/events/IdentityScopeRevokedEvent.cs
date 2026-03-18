namespace Whycespace.Domain.Identity.Events;

public sealed record IdentityScopeRevokedEvent(
    Guid EventId,
    Guid IdentityId,
    string ScopeKey,
    Guid RevokedBy,
    DateTime RevokedAt,
    int EventVersion);
