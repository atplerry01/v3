namespace Whycespace.Systems.WhyceID.Models;

public sealed record IdentityPolicyContext(
    Guid IdentityId,
    IdentityStatus IdentityStatus,
    int TrustScore,
    IReadOnlyCollection<string> Roles,
    IReadOnlyDictionary<string, string> Attributes,
    int DeviceTrustLevel,
    int SessionTrustLevel,
    string RequestedOperation,
    string RequestSource,
    DateTime RequestTimestamp
);
