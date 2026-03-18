namespace Whycespace.Domain.Events.Core.Identity;

public sealed record SessionCreatedEvent(
    Guid EventId,
    Guid SessionId,
    Guid IdentityId,
    DateTime IssuedAt,
    DateTime ExpiresAt,
    int EventVersion);
