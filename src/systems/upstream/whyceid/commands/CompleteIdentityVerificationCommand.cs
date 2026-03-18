namespace Whycespace.Systems.WhyceID.Commands;

using Whycespace.Systems.WhyceID.Models;

public sealed record CompleteIdentityVerificationCommand(
    Guid IdentityId,
    VerificationType VerificationType,
    Guid VerifiedBy,
    DateTime Timestamp);
