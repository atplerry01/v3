namespace Whycespace.Engines.T3I.WhyceChain;

using Whycespace.Systems.Upstream.WhyceChain.Models;

public sealed record ChainReplicationCommand(
    IReadOnlyList<ChainBlock> SourceBlocks,
    IReadOnlyList<ChainLedgerEntry> SourceLedgerEntries,
    long TargetHeight,
    string TraceId,
    string CorrelationId,
    DateTime Timestamp);
