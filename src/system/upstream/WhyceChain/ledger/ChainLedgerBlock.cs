namespace Whycespace.System.Upstream.WhyceChain.Ledger;

/// <summary>
/// Logical grouping of ledger entries with Merkle root anchoring.
/// Entries are ordered by sequence number. MerkleRoot is generated
/// from entry hashes. BlockHash is deterministically computed.
/// </summary>
public sealed record ChainLedgerBlock(
    Guid BlockId,
    long BlockHeight,
    IReadOnlyList<ChainLedgerEntry> Entries,
    string MerkleRoot,
    string PreviousBlockHash,
    string BlockHash,
    DateTimeOffset CreatedAt);
