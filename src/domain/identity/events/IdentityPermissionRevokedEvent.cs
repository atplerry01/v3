namespace Whycespace.Domain.Events.Core.Identity;

public sealed record IdentityPermissionRevokedEvent(
    Guid EventId,
    Guid IdentityId,
    string PermissionKey,
    Guid RevokedBy,
    DateTime RevokedAt,
    int EventVersion);
