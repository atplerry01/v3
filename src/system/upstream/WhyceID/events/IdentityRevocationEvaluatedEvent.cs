namespace Whycespace.System.WhyceID.Events;

public sealed record IdentityRevocationEvaluatedEvent(
    Guid EventId,
    Guid IdentityId,
    string RevocationId,
    bool Approved,
    string RevocationScope,
    string RevocationReason,
    int RiskScore,
    DateTime Timestamp);
