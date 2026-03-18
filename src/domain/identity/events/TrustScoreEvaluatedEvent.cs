namespace Whycespace.Domain.Events.Core.Identity;

public sealed record TrustScoreEvaluatedEvent(
    Guid EventId,
    Guid IdentityId,
    double TrustScore,
    DateTime EvaluatedAt,
    int EventVersion);
