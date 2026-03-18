namespace Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyImpactForecastRequest(
    string Domain,
    IReadOnlyList<PolicySimulationRequest> SimulationContexts
);
