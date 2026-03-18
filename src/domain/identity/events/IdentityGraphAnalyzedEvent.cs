namespace Whycespace.Domain.Identity.Events;

public sealed record IdentityGraphAnalyzedEvent(
    Guid EventId,
    Guid IdentityId,
    double RiskScore,
    int SuspiciousConnections,
    DateTime AnalyzedAt,
    int EventVersion);
