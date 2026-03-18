namespace Whycespace.Systems.Upstream.WhyceChain.Events;

public sealed record ChainIntegrityVerifiedEvent(
    Guid EventId,
    bool IsValid,
    int BlocksVerified,
    int FailedBlocks,
    DateTimeOffset Timestamp);
