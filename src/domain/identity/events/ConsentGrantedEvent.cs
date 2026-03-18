namespace Whycespace.Domain.Identity.Events;

public sealed record ConsentGrantedEvent(
    Guid EventId,
    Guid IdentityId,
    string ConsentType,
    string Scope,
    DateTime GrantedAt,
    int EventVersion);
