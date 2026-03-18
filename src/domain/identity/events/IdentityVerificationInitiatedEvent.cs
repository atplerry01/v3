namespace Whycespace.Domain.Identity.Events;

public sealed record IdentityVerificationInitiatedEvent(
    Guid EventId,
    Guid IdentityId,
    string VerificationType,
    Guid RequestedBy,
    DateTime RequestedAt,
    int EventVersion);
