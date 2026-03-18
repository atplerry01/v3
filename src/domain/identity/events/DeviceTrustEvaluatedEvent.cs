namespace Whycespace.Domain.Events.Core.Identity;

public sealed record DeviceTrustEvaluatedEvent(
    Guid EventId,
    Guid IdentityId,
    string DeviceId,
    double TrustScore,
    string TrustLevel,
    DateTime EvaluatedAt,
    int EventVersion);
