namespace Whycespace.System.WhyceID.Commands;

public sealed record RevokeIdentityRoleCommand(
    Guid IdentityId,
    string RoleId,
    Guid RevokedBy,
    DateTime Timestamp);
