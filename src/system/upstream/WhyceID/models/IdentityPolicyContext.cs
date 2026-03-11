namespace Whycespace.System.WhyceID.Models;

public sealed record IdentityPolicyContext(
    Guid IdentityId,
    IReadOnlyCollection<string> Roles,
    int TrustScore,
    bool Verified,
    bool Revoked
);
