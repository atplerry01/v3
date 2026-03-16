namespace Whycespace.Systems.Upstream.WhyceChain.Ledger;

public sealed record EvidenceHashResult(
    string EvidenceHash,
    string HashAlgorithm,
    string PayloadCanonicalHash,
    string MetadataHash,
    DateTime GeneratedAt,
    string TraceId);
