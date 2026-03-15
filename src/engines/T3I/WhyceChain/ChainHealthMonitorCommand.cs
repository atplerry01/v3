namespace Whycespace.Engines.T3I.WhyceChain;

using Whycespace.System.Upstream.WhyceChain.Models;

public sealed record ChainHealthMonitorCommand(
    IReadOnlyList<ChainBlock> Blocks,
    IReadOnlyList<ChainLedgerEntry> LedgerEntries,
    long LatestSnapshotHeight,
    long ReplicationHeight,
    long AnchoredBlockHeight,
    string TraceId,
    string CorrelationId,
    DateTime Timestamp);
