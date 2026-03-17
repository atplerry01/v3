namespace Whycespace.Engines.T3I.Reporting.Chain;

using Whycespace.Systems.Upstream.WhyceChain.Models;

public sealed record ChainIndexCommand(
    IReadOnlyList<ChainLedgerEntry> LedgerEntries,
    IReadOnlyList<ChainBlock> Blocks,
    string TraceId,
    string CorrelationId,
    DateTime Timestamp);
