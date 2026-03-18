using Whycespace.Contracts.Policy;

namespace Whycespace.Systems.Downstream.Work.Shared.Policy;

public static class WorkPolicyDecisionMapper
{
    public static WorkPolicyOutcome Map(PolicyEvaluationResult result)
    {
        if (result.IsPermitted)
            return WorkPolicyOutcome.Allowed();

        var reasons = result.Violations.Count > 0
            ? string.Join("; ", result.Violations)
            : "Policy evaluation denied the request.";

        return WorkPolicyOutcome.Denied(reasons);
    }
}

public sealed record WorkPolicyOutcome(bool IsAllowed, string? DenialReason = null)
{
    public static WorkPolicyOutcome Allowed() => new(true);
    public static WorkPolicyOutcome Denied(string reason) => new(false, reason);
}
