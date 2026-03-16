namespace Whycespace.Systems.Upstream.WhyceChain.Ledger;

public sealed record ChainAppendCommand(
    long CurrentChainHeight,
    string LatestBlockHash,
    ChainBlock NewBlock,
    string TraceId,
    string CorrelationId,
    DateTime Timestamp);
