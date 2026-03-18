namespace Whycespace.Engines.T3I.Reporting.Chain.Models;

using Whycespace.Systems.Upstream.WhyceChain.Ledger;

public sealed record ChainRecoveryResult(
    IReadOnlyList<ChainBlock> RecoveredBlocks,
    IReadOnlyList<ChainLedgerEntry> RecoveredEntries,
    long RecoveredHeight,
    string RecoveryHash,
    int RecoveredEntryCount,
    DateTime GeneratedAt,
    string TraceId);
