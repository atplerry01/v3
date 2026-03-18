namespace Whycespace.Engines.T3I.Forecasting.Policy.Models;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyImpactForecastInput(
    IReadOnlyList<PolicyDefinition> CurrentPolicies,
    IReadOnlyList<PolicyDefinition> ProposedPolicies,
    IReadOnlyList<PolicyContext> SimulationContexts
);
