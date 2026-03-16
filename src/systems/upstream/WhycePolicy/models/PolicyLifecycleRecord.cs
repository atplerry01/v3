namespace Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyLifecycleRecord(
    string PolicyId,
    string Version,
    PolicyLifecycleState State,
    DateTime UpdatedAt
);
