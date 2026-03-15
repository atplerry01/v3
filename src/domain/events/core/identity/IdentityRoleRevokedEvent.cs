namespace Whycespace.Domain.Events.Core.Identity;

public sealed record IdentityRoleRevokedEvent(
    Guid EventId,
    Guid IdentityId,
    string RoleId,
    Guid RevokedBy,
    DateTime RevokedAt,
    int EventVersion);
