namespace Whycespace.System.WhyceID.Commands;

using Whycespace.System.WhyceID.Models;

public sealed record CompleteIdentityVerificationCommand(
    Guid IdentityId,
    VerificationType VerificationType,
    Guid VerifiedBy,
    DateTime Timestamp);
