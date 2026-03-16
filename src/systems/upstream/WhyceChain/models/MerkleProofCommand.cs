namespace Whycespace.Systems.Upstream.WhyceChain.Models;

public sealed record MerkleProofCommand(
    string EntryHash,
    IReadOnlyList<string> BlockEntries,
    string MerkleRoot,
    string TraceId,
    string CorrelationId,
    DateTime Timestamp);
