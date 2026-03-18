namespace Whycespace.Domain.Identity.Events;

public sealed record IdentityRoleRevokedEvent(
    Guid EventId,
    Guid IdentityId,
    string RoleId,
    Guid RevokedBy,
    DateTime RevokedAt,
    int EventVersion);
