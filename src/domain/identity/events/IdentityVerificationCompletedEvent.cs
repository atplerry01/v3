namespace Whycespace.Domain.Events.Core.Identity;

public sealed record IdentityVerificationCompletedEvent(
    Guid EventId,
    Guid IdentityId,
    string VerificationType,
    Guid VerifiedBy,
    DateTime VerifiedAt,
    int EventVersion);
