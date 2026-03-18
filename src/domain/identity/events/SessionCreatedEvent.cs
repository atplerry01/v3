namespace Whycespace.Domain.Identity.Events;

public sealed record SessionCreatedEvent(
    Guid EventId,
    Guid SessionId,
    Guid IdentityId,
    DateTime IssuedAt,
    DateTime ExpiresAt,
    int EventVersion);
