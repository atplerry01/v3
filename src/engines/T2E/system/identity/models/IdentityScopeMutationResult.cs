namespace Whycespace.Engines.T2E.System.Identity.Models;

public sealed record IdentityScopeMutationResult(
    Guid IdentityId,
    string ScopeKey,
    ScopeMutationType MutationType,
    Guid ExecutedBy,
    DateTime ExecutedAt);
