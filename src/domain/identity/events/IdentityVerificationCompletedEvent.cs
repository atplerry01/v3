namespace Whycespace.Domain.Identity.Events;

public sealed record IdentityVerificationCompletedEvent(
    Guid EventId,
    Guid IdentityId,
    string VerificationType,
    Guid VerifiedBy,
    DateTime VerifiedAt,
    int EventVersion);
