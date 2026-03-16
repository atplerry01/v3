namespace Whycespace.Systems.WhyceID.Commands;

using Whycespace.Systems.WhyceID.Models;

public sealed record RejectIdentityVerificationCommand(
    Guid IdentityId,
    VerificationType VerificationType,
    Guid RejectedBy,
    string Reason,
    DateTime Timestamp);
