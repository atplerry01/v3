namespace Whycespace.Engines.T3I.Monitoring.Chain;

using Whycespace.Systems.Upstream.WhyceChain.Models;

public sealed record ChainHealthMonitorCommand(
    IReadOnlyList<ChainBlock> Blocks,
    IReadOnlyList<ChainLedgerEntry> LedgerEntries,
    long LatestSnapshotHeight,
    long ReplicationHeight,
    long AnchoredBlockHeight,
    string TraceId,
    string CorrelationId,
    DateTime Timestamp);
