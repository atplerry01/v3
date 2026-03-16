namespace Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyDependency(
    string PolicyId,
    string DependsOnPolicyId
);
