namespace Whycespace.Engines.T0U.WhycePolicy;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyEvaluationInput(
    IReadOnlyList<PolicyDefinition> Policies,
    PolicyContext PolicyContext
);
