namespace Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyImpactForecastRequest(
    string Domain,
    IReadOnlyList<PolicySimulationRequest> SimulationContexts
);
