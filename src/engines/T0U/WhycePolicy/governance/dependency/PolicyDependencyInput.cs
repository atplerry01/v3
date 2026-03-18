namespace Whycespace.Engines.T0U.WhycePolicy.Governance.Dependency;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyDependencyInput(
    IReadOnlyList<PolicyDefinition> Policies,
    Dictionary<string, List<string>> PolicyDependencies
);
