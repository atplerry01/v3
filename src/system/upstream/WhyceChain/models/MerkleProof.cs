namespace Whycespace.System.Upstream.WhyceChain.Models;

public sealed record MerkleProof(
    string RootHash,
    string LeafHash,
    IReadOnlyList<string> ProofPath);
