namespace Whycespace.Domain.Events.Core.Identity;

public sealed record IdentityPermissionGrantedEvent(
    Guid EventId,
    Guid IdentityId,
    string PermissionKey,
    Guid GrantedBy,
    DateTime GrantedAt,
    int EventVersion);
