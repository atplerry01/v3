namespace Whycespace.Systems.Upstream.WhyceID.Simulation;

public sealed record IdentitySimulationRequest(
    string Operation,
    string IdentityType,
    IReadOnlyDictionary<string, object> Attributes);

public sealed record IdentitySimulationResult(
    string Operation,
    bool WouldSucceed,
    string ProjectedStatus,
    IReadOnlyList<string> PolicyViolations,
    bool IsDryRun,
    DateTimeOffset SimulatedAt);

public sealed class IdentitySimulator
{
    public IdentitySimulationResult Simulate(IdentitySimulationRequest request)
    {
        var violations = new List<string>();

        if (request.Operation == "create" && !request.Attributes.ContainsKey("displayName"))
            violations.Add("displayName is required for identity creation");

        return new IdentitySimulationResult(
            request.Operation,
            WouldSucceed: violations.Count == 0,
            ProjectedStatus: violations.Count == 0 ? "Active" : "Rejected",
            PolicyViolations: violations,
            IsDryRun: true,
            SimulatedAt: DateTimeOffset.UtcNow);
    }
}
