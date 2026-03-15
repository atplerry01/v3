namespace Whycespace.System.Upstream.WhycePolicy.Registry;

using Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyRegistryEntry(
    string PolicyId,
    string PolicyName,
    string Domain,
    int Priority,
    PolicyLifecycleState LifecycleState,
    int CurrentVersion,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
