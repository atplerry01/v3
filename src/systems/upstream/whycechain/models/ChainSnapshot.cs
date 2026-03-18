namespace Whycespace.Systems.Upstream.WhyceChain.Models;

public sealed record ChainSnapshot(
    string SnapshotId,
    long LatestBlockNumber,
    string LatestBlockHash,
    int TotalEntries,
    DateTimeOffset CreatedAt);
