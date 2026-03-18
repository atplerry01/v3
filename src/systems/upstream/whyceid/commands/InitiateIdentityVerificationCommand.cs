namespace Whycespace.Systems.WhyceID.Commands;

using Whycespace.Systems.WhyceID.Models;

public sealed record InitiateIdentityVerificationCommand(
    Guid IdentityId,
    VerificationType VerificationType,
    Guid RequestedBy,
    DateTime Timestamp);
