namespace Whycespace.Systems.Upstream.WhyceChain.Events;

public sealed record EvidenceAnchoredEvent(
    Guid EventId,
    Guid EvidenceId,
    string EvidenceHash,
    Guid BlockId,
    DateTimeOffset Timestamp);
