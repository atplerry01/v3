namespace Whycespace.Engines.T0U.WhycePolicy;

using Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyDependencyResult(
    List<PolicyDefinition> OrderedPolicies,
    List<List<string>> DetectedCycles,
    List<string> MissingDependencies
);
