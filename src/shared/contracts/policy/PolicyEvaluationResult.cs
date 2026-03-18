namespace Whycespace.Contracts.Policy;

public sealed record PolicyEvaluationResult(
    bool IsPermitted,
    IReadOnlyList<PolicyDecision> Decisions,
    IReadOnlyList<string> Violations
)
{
    public static PolicyEvaluationResult Permit(params PolicyDecision[] decisions)
        => new(true, decisions, Array.Empty<string>());

    public static PolicyEvaluationResult Deny(IReadOnlyList<string> violations, params PolicyDecision[] decisions)
        => new(false, decisions, violations);
}
