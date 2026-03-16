namespace Whycespace.Systems.Upstream.WhyceChain.Ledger;

public sealed record ChainBlock(
    Guid BlockId,
    long BlockHeight,
    string? PreviousBlockHash,
    IReadOnlyList<ChainLedgerEntry> Entries,
    string MerkleRoot,
    string BlockHash,
    int EntryCount,
    DateTime CreatedAt,
    string ValidatorSignature,
    string TraceId);
