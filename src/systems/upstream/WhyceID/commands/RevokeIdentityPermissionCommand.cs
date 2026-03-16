namespace Whycespace.Systems.WhyceID.Commands;

public sealed record RevokeIdentityPermissionCommand(
    Guid IdentityId,
    string PermissionKey,
    Guid RevokedBy,
    DateTime Timestamp);
