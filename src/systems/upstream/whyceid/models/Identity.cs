namespace Whycespace.Systems.WhyceID.Models;

public sealed record Identity(
    Guid IdentityId,
    IdentityType IdentityType,
    IdentityStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<IdentityRole> Roles,
    IReadOnlyList<IdentityAttribute> Attributes,
    IReadOnlyList<IdentityVerification> Verifications,
    double TrustScore,
    string? ExternalFederationId,
    bool DeviceTrustEnabled,
    bool SessionEnabled);
