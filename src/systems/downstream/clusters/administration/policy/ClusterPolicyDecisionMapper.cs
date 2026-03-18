using Whycespace.Contracts.Policy;

namespace Whycespace.Systems.Downstream.Clusters.Administration.Policy;

public static class ClusterPolicyDecisionMapper
{
    public static ClusterPolicyOutcome Map(PolicyEvaluationResult result)
    {
        if (result.IsPermitted)
            return ClusterPolicyOutcome.Allowed();

        var reasons = result.Violations.Count > 0
            ? string.Join("; ", result.Violations)
            : "Cluster policy evaluation denied the request.";

        return ClusterPolicyOutcome.Denied(reasons);
    }
}

public sealed record ClusterPolicyOutcome(bool IsAllowed, string? DenialReason = null)
{
    public static ClusterPolicyOutcome Allowed() => new(true);
    public static ClusterPolicyOutcome Denied(string reason) => new(false, reason);
}
