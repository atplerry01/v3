namespace Whycespace.Domain.Events.Core.Identity;

public sealed record ConsentGrantedEvent(
    Guid EventId,
    Guid IdentityId,
    string ConsentType,
    string Scope,
    DateTime GrantedAt,
    int EventVersion);
