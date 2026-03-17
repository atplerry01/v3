namespace Whycespace.Engines.T3I.Forecasting.Policy;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyImpactForecastInput(
    IReadOnlyList<PolicyDefinition> CurrentPolicies,
    IReadOnlyList<PolicyDefinition> ProposedPolicies,
    IReadOnlyList<PolicyContext> SimulationContexts
);
