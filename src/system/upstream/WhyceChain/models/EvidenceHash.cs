namespace Whycespace.System.Upstream.WhyceChain.Models;

public sealed record EvidenceHash(
    string Hash,
    string Algorithm,
    DateTimeOffset Timestamp);
