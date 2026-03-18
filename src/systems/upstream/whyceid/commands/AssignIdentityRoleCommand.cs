namespace Whycespace.Systems.WhyceID.Commands;

public sealed record AssignIdentityRoleCommand(
    Guid IdentityId,
    string RoleId,
    string RoleName,
    Guid GrantedBy,
    DateTime Timestamp);
