namespace Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyConflict(
    string PolicyA,
    string PolicyB,
    string Domain,
    string Reason,
    DateTime DetectedAt
);
