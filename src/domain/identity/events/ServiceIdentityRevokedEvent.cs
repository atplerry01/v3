namespace Whycespace.Domain.Identity.Events;

public sealed record ServiceIdentityRevokedEvent(
    Guid EventId,
    Guid ServiceIdentityId,
    string Reason,
    DateTime RevokedAt,
    int EventVersion);
