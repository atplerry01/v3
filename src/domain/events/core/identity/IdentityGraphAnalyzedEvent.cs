namespace Whycespace.Domain.Events.Core.Identity;

public sealed record IdentityGraphAnalyzedEvent(
    Guid EventId,
    Guid IdentityId,
    double RiskScore,
    int SuspiciousConnections,
    DateTime AnalyzedAt,
    int EventVersion);
