namespace Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyVersion(
    string PolicyId,
    int Version,
    DateTime CreatedAt,
    PolicyStatus Status
);
