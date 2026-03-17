namespace Whycespace.Engines.T2E.System.Identity.Models;

public sealed record SessionValidationResult(
    Guid SessionId,
    bool Valid,
    Guid IdentityId,
    string Reason,
    DateTime ValidatedAt);
