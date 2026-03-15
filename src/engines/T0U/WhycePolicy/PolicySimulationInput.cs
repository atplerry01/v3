namespace Whycespace.Engines.T0U.WhycePolicy;

using Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicySimulationInput(
    IReadOnlyList<PolicyDefinition> Policies,
    IReadOnlyList<PolicyContext> SimulationContexts
);
