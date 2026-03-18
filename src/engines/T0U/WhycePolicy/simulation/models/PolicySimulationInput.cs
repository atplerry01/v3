namespace Whycespace.Engines.T0U.WhycePolicy.Simulation.Models;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicySimulationInput(
    IReadOnlyList<PolicyDefinition> Policies,
    IReadOnlyList<PolicyContext> SimulationContexts
);
