namespace Whycespace.Engines.T3I.WhycePolicy;

using Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyImpactForecastInput(
    IReadOnlyList<PolicyDefinition> CurrentPolicies,
    IReadOnlyList<PolicyDefinition> ProposedPolicies,
    IReadOnlyList<PolicyContext> SimulationContexts
);
