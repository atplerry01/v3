namespace Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyDependency(
    string PolicyId,
    string DependsOnPolicyId
);
