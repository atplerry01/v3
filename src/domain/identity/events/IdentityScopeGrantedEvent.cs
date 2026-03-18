namespace Whycespace.Domain.Identity.Events;

public sealed record IdentityScopeGrantedEvent(
    Guid EventId,
    Guid IdentityId,
    string ScopeKey,
    Guid GrantedBy,
    DateTime GrantedAt,
    int EventVersion);
