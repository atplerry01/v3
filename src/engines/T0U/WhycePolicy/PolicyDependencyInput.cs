namespace Whycespace.Engines.T0U.WhycePolicy;

using Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyDependencyInput(
    IReadOnlyList<PolicyDefinition> Policies,
    Dictionary<string, List<string>> PolicyDependencies
);
