namespace Whycespace.Domain.Identity.Events;

public sealed record IdentityRoleAssignedEvent(
    Guid EventId,
    Guid IdentityId,
    string RoleId,
    string RoleName,
    Guid GrantedBy,
    DateTime GrantedAt,
    int EventVersion);
