namespace Whycespace.Systems.Upstream.WhyceChain.Events;

public sealed record ChainSnapshotCreatedEvent(
    Guid EventId,
    Guid SnapshotId,
    int BlockCount,
    string LatestHash,
    DateTimeOffset Timestamp);
