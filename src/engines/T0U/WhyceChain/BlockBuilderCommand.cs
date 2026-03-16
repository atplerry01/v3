namespace Whycespace.Engines.T0U.WhyceChain;

using Whycespace.Systems.Upstream.WhyceChain.Ledger;

public sealed record BlockBuilderCommand(
    long BlockHeight,
    string PreviousBlockHash,
    IReadOnlyList<ChainLedgerEntry> LedgerEntries,
    string TraceId,
    string CorrelationId,
    DateTime Timestamp);
