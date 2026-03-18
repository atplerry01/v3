namespace Whycespace.Systems.Upstream.WhycePolicy.Simulation;

public sealed record PolicySimulationDryRunRequest(
    string PolicyId,
    string Domain,
    string Operation,
    IReadOnlyDictionary<string, object> Attributes);

public sealed record PolicySimulationDryRunResult(
    string PolicyId,
    bool WouldAllow,
    string ProjectedAction,
    string ProjectedReason,
    IReadOnlyList<string> MatchedRules,
    bool IsDryRun,
    DateTimeOffset SimulatedAt);

public sealed class PolicySimulator
{
    public PolicySimulationDryRunResult Simulate(PolicySimulationDryRunRequest request)
    {
        var matchedRules = new List<string>();

        if (request.Domain.StartsWith("governance"))
            matchedRules.Add("constitutional.governance.access");
        if (request.Operation.Contains("create") || request.Operation.Contains("register"))
            matchedRules.Add("constitutional.mutation.guard");

        var wouldAllow = matchedRules.Count > 0;

        return new PolicySimulationDryRunResult(
            request.PolicyId,
            WouldAllow: wouldAllow,
            ProjectedAction: wouldAllow ? "Allow" : "Deny",
            ProjectedReason: wouldAllow ? "All policy rules satisfied" : "No matching rules found",
            MatchedRules: matchedRules,
            IsDryRun: true,
            SimulatedAt: DateTimeOffset.UtcNow);
    }
}
