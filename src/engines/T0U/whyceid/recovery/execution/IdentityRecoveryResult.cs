namespace Whycespace.Engines.T0U.WhyceID.Recovery.Execution;

public sealed record IdentityRecoveryResult(
    string RecoveryId,
    Guid IdentityId,
    bool Approved,
    RecoveryMethod RecoveryMethod,
    double RiskScore,
    bool RequiresAdditionalVerification,
    string Reason,
    DateTime EvaluatedAt
);
