namespace Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyEnforcementResult(
    bool Allowed,
    string Reason,
    IReadOnlyList<PolicyDecision> Decisions,
    DateTime EvaluatedAt
);
