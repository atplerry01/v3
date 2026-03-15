namespace Whycespace.System.WhyceID.Commands;

public sealed record GrantIdentityPermissionCommand(
    Guid IdentityId,
    string PermissionKey,
    Guid GrantedBy,
    DateTime Timestamp);
