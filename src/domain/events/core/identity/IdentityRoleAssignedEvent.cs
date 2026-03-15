namespace Whycespace.Domain.Events.Core.Identity;

public sealed record IdentityRoleAssignedEvent(
    Guid EventId,
    Guid IdentityId,
    string RoleId,
    string RoleName,
    Guid GrantedBy,
    DateTime GrantedAt,
    int EventVersion);
