namespace Whycespace.Engines.T3I.WhyceChain;

using Whycespace.System.Upstream.WhyceChain.Models;

public sealed record ChainIndexCommand(
    IReadOnlyList<ChainLedgerEntry> LedgerEntries,
    IReadOnlyList<ChainBlock> Blocks,
    string TraceId,
    string CorrelationId,
    DateTime Timestamp);
