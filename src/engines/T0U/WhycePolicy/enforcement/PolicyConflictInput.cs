namespace Whycespace.Engines.T0U.WhycePolicy.Enforcement;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyConflictInput(
    IReadOnlyList<PolicyDefinition> Policies
);
