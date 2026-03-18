namespace Whycespace.Engines.T0U.WhyceID.Revocation.Execution;

public sealed record IdentityRevocationResult(
    string RevocationId,
    Guid IdentityId,
    bool Approved,
    RevocationScope RevocationScope,
    RevocationReason RevocationReason,
    int RiskScore,
    bool RequiresGovernanceReview,
    string Reason,
    DateTime EvaluatedAt);
