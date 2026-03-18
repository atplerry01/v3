using Whycespace.Contracts.Policy;

namespace Whycespace.Systems.Downstream.Spv.Governance.Policy;

public static class SpvPolicyDecisionMapper
{
    public static SpvPolicyOutcome Map(PolicyEvaluationResult result)
    {
        if (result.IsPermitted)
            return SpvPolicyOutcome.Allowed();

        var reasons = result.Violations.Count > 0
            ? string.Join("; ", result.Violations)
            : "SPV policy evaluation denied the request.";

        return SpvPolicyOutcome.Denied(reasons);
    }
}

public sealed record SpvPolicyOutcome(bool IsAllowed, string? DenialReason = null)
{
    public static SpvPolicyOutcome Allowed() => new(true);
    public static SpvPolicyOutcome Denied(string reason) => new(false, reason);
}
