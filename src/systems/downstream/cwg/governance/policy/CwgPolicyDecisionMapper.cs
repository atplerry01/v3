using Whycespace.Contracts.Policy;

namespace Whycespace.Systems.Downstream.Cwg.Governance.Policy;

public static class CwgPolicyDecisionMapper
{
    public static CwgPolicyOutcome Map(PolicyEvaluationResult result)
    {
        if (result.IsPermitted)
            return CwgPolicyOutcome.Allowed();

        var reasons = result.Violations.Count > 0
            ? string.Join("; ", result.Violations)
            : "CWG policy evaluation denied the request.";

        return CwgPolicyOutcome.Denied(reasons);
    }
}

public sealed record CwgPolicyOutcome(bool IsAllowed, string? DenialReason = null)
{
    public static CwgPolicyOutcome Allowed() => new(true);
    public static CwgPolicyOutcome Denied(string reason) => new(false, reason);
}
