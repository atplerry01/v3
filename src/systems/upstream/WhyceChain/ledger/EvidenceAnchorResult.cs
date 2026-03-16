namespace Whycespace.Systems.Upstream.WhyceChain.Ledger;

public sealed record EvidenceAnchorResult(
    string BlockHash,
    string MerkleRoot,
    string AnchorPayload,
    string AnchorPayloadHash,
    string AnchorTarget,
    string AnchorReferenceId,
    DateTime GeneratedAt,
    string TraceId);
