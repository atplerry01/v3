namespace Whycespace.Domain.Identity.Events;

public sealed record TrustScoreEvaluatedEvent(
    Guid EventId,
    Guid IdentityId,
    double TrustScore,
    DateTime EvaluatedAt,
    int EventVersion);
