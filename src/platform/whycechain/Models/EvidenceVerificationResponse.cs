namespace Whycespace.Platform.WhyceChain.Models;

public sealed record EvidenceVerificationResponse(
    bool EvidenceExists,
    bool MerkleProofValid,
    bool BlockIntegrityValid,
    DateTime VerificationTimestamp,
    string TraceId);
