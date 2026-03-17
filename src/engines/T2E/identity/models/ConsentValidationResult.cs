namespace Whycespace.Engines.T2E.Identity.Models;

public sealed record ConsentValidationResult(
    Guid IdentityId,
    string ConsentType,
    bool Valid,
    bool ScopeValidated,
    string Reason,
    DateTime ValidatedAt);
