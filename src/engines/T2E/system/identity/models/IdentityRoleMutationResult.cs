namespace Whycespace.Engines.T2E.System.Identity.Models;

public sealed record IdentityRoleMutationResult(
    Guid IdentityId,
    string RoleId,
    string RoleName,
    RoleMutationType MutationType,
    Guid ExecutedBy,
    DateTime ExecutedAt);
