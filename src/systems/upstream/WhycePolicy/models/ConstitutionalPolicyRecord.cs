namespace Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record ConstitutionalPolicyRecord(
    string PolicyId,
    string Version,
    string ProtectionLevel,
    DateTime RegisteredAt
);
