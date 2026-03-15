namespace Whycespace.System.WhyceID.Adapters;

public sealed record IdentityPolicyDecision(
    string PolicyId,
    bool Allowed,
    IdentityPolicyAction Action,
    string Reason,
    DateTime EvaluatedAt
);

public enum IdentityPolicyAction
{
    Allow,
    Deny,
    RequireAdditionalVerification,
    EscalateGovernanceReview
}
