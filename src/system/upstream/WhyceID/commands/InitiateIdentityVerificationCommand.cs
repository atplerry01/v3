namespace Whycespace.System.WhyceID.Commands;

using Whycespace.System.WhyceID.Models;

public sealed record InitiateIdentityVerificationCommand(
    Guid IdentityId,
    VerificationType VerificationType,
    Guid RequestedBy,
    DateTime Timestamp);
