namespace Whycespace.Engines.T2E.System.Identity.Models;

public sealed record IdentityPermissionMutationResult(
    Guid IdentityId,
    string PermissionKey,
    PermissionMutationType MutationType,
    Guid ExecutedBy,
    DateTime ExecutedAt);
