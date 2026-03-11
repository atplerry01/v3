namespace Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyDomainBinding(
    string PolicyId,
    string Version,
    string Domain,
    DateTime BoundAt
);
