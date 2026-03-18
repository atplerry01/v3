namespace Whycespace.Systems.Upstream.WhyceChain.Models;

public sealed record MerkleProofResult(
    IReadOnlyList<string> ProofPath,
    string ComputedRoot,
    bool ProofValid,
    int ProofDepth,
    DateTime GeneratedAt,
    string TraceId);
