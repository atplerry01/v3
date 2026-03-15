namespace Whycespace.System.Upstream.WhyceChain.Models;

public sealed record IntegrityVerificationCommand(
    IReadOnlyList<ChainLedgerEntry> LedgerEntries,
    IReadOnlyList<ChainBlock> Blocks,
    MerkleProof? MerkleProof,
    string TraceId,
    string CorrelationId,
    DateTimeOffset Timestamp);
