namespace Whycespace.Systems.Upstream.WhyceChain.Ledger;

public sealed record EvidenceAnchorCommand(
    ChainBlock Block,
    string AnchorTarget,
    string TraceId,
    string CorrelationId,
    DateTime Timestamp);
