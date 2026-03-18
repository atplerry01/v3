namespace Whycespace.Domain.Events.Core.Identity;

public sealed record IdentityScopeGrantedEvent(
    Guid EventId,
    Guid IdentityId,
    string ScopeKey,
    Guid GrantedBy,
    DateTime GrantedAt,
    int EventVersion);
