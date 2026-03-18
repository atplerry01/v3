namespace Whycespace.Systems.Upstream.WhyceChain.Events;

public sealed record ChainBlockAppendedEvent(
    Guid EventId,
    Guid BlockId,
    string Hash,
    string PreviousHash,
    int EntryCount,
    DateTimeOffset Timestamp);
