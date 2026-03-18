namespace Whycespace.Systems.WhyceID.Commands;

public sealed record RevokeIdentityRoleCommand(
    Guid IdentityId,
    string RoleId,
    Guid RevokedBy,
    DateTime Timestamp);
