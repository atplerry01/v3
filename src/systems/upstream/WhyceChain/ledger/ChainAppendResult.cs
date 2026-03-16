namespace Whycespace.Systems.Upstream.WhyceChain.Ledger;

public sealed record ChainAppendResult(
    bool BlockAccepted,
    long NewChainHeight,
    string AppendedBlockHash,
    bool ChainContinuityValid,
    DateTime GeneratedAt,
    string TraceId,
    IReadOnlyList<string> ValidationErrors);
