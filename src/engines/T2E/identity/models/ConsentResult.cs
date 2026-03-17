namespace Whycespace.Engines.T2E.Identity.Models;

public sealed record ConsentResult(
    Guid IdentityId,
    string ConsentType,
    bool Granted,
    string Scope,
    DateTime GrantedAt);
