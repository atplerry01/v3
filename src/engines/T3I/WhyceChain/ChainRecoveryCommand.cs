namespace Whycespace.Engines.T3I.WhyceChain;

using Whycespace.Systems.Upstream.WhyceChain.Ledger;

public sealed record ChainRecoveryCommand(
    long SnapshotHeight,
    string SnapshotBlockHash,
    IReadOnlyList<ChainBlock> ReplicatedBlocks,
    IReadOnlyList<ChainLedgerEntry> ReplicatedLedgerEntries,
    string TraceId,
    string CorrelationId,
    DateTime Timestamp);
