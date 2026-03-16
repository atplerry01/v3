namespace Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicySimulationResult(
    string Domain,
    string ActorId,
    IReadOnlyList<PolicyDecision> Decisions,
    DateTime SimulatedAt
);
