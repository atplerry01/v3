namespace Whycespace.Engines.T2E.Identity.Models;

public sealed record SessionResult(
    Guid SessionId,
    Guid IdentityId,
    string SessionToken,
    DateTime IssuedAt,
    DateTime ExpiresAt,
    bool Active);
