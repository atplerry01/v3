namespace Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record ConstitutionalPolicyRecord(
    string PolicyId,
    string Version,
    string ProtectionLevel,
    DateTime RegisteredAt
);
