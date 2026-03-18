namespace Whycespace.Engines.T0U.WhyceID.Recovery.Request;

public enum RecoveryMethod
{
    EmailVerification,
    PhoneVerification,
    GuardianVerification,
    DocumentVerification,
    MultiFactorRecovery
}

public sealed record IdentityRecoveryRequest(
    Guid IdentityId,
    DateTime RequestedAt,
    RecoveryMethod RecoveryMethod,
    string VerificationEvidence,
    string DeviceFingerprint,
    string RequestIp,
    string RequestLocation
);
