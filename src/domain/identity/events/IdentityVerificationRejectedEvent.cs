namespace Whycespace.Domain.Identity.Events;

public sealed record IdentityVerificationRejectedEvent(
    Guid EventId,
    Guid IdentityId,
    string VerificationType,
    Guid RejectedBy,
    string Reason,
    DateTime RejectedAt,
    int EventVersion);
