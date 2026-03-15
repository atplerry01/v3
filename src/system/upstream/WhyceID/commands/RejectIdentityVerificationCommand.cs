namespace Whycespace.System.WhyceID.Commands;

using Whycespace.System.WhyceID.Models;

public sealed record RejectIdentityVerificationCommand(
    Guid IdentityId,
    VerificationType VerificationType,
    Guid RejectedBy,
    string Reason,
    DateTime Timestamp);
