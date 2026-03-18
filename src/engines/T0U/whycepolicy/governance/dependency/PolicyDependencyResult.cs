namespace Whycespace.Engines.T0U.WhycePolicy.Governance.Dependency;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyDependencyResult(
    List<PolicyDefinition> OrderedPolicies,
    List<List<string>> DetectedCycles,
    List<string> MissingDependencies
);
