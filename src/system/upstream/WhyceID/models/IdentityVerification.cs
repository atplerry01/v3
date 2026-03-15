namespace Whycespace.System.WhyceID.Models;

public enum VerificationType
{
    Email = 0,
    Phone = 1,
    Document = 2,
    Biometric = 3
}

public sealed record IdentityVerification(
    VerificationType VerificationType,
    VerificationStatus Status,
    DateTime? VerifiedAt,
    string Verifier);
