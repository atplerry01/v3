namespace Whycespace.Engines.T2E.System.Identity.Models;

public sealed record ConsentResult(
    Guid IdentityId,
    string ConsentType,
    bool Granted,
    string Scope,
    DateTime GrantedAt);
