namespace Whycespace.Engines.T3I.Reporting.Chain;

using Whycespace.Systems.Upstream.WhyceChain.Models;

public sealed record ChainReplicationResult(
    IReadOnlyList<ChainBlock> ReplicationBlocks,
    IReadOnlyList<ChainLedgerEntry> ReplicationEntries,
    long ReplicatedHeight,
    string ReplicationHash,
    int ReplicationEntryCount,
    DateTime GeneratedAt,
    string TraceId);
