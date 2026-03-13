namespace Whycespace.System.WhyceID.Models;

public sealed record IdentityConsent(
    Guid ConsentId,
    Guid IdentityId,
    string Target,
    string Scope,
    DateTime GrantedAt,
    bool Revoked);
