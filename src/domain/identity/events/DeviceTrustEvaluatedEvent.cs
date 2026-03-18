namespace Whycespace.Domain.Identity.Events;

public sealed record DeviceTrustEvaluatedEvent(
    Guid EventId,
    Guid IdentityId,
    string DeviceId,
    double TrustScore,
    string TrustLevel,
    DateTime EvaluatedAt,
    int EventVersion);
