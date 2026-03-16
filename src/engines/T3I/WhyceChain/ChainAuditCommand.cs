namespace Whycespace.Engines.T3I.WhyceChain;

using Whycespace.Systems.Upstream.WhyceChain.Models;

public sealed record ChainAuditCommand(
    IReadOnlyList<ChainLedgerEntry> LedgerEntries,
    IReadOnlyList<ChainBlock> Blocks,
    long? SnapshotHeight,
    string TraceId,
    string CorrelationId,
    DateTime Timestamp);
