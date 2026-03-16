namespace Whycespace.Systems.Upstream.WhyceChain.Models;

public sealed record EvidenceHash(
    string Hash,
    string Algorithm,
    DateTimeOffset Timestamp);
