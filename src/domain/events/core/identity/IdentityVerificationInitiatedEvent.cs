namespace Whycespace.Domain.Events.Core.Identity;

public sealed record IdentityVerificationInitiatedEvent(
    Guid EventId,
    Guid IdentityId,
    string VerificationType,
    Guid RequestedBy,
    DateTime RequestedAt,
    int EventVersion);
