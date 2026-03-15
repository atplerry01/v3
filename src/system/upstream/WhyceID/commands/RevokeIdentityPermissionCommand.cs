namespace Whycespace.System.WhyceID.Commands;

public sealed record RevokeIdentityPermissionCommand(
    Guid IdentityId,
    string PermissionKey,
    Guid RevokedBy,
    DateTime Timestamp);
