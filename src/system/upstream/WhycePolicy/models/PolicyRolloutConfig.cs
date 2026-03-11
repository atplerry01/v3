namespace Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyRolloutConfig(
    string PolicyId,
    string Version,
    PolicyRolloutStrategy Strategy,
    int Percentage,
    IReadOnlyList<string> Actors,
    IReadOnlyList<string> Domains,
    DateTime CreatedAt
);
