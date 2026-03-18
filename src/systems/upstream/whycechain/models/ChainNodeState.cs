namespace Whycespace.Systems.Upstream.WhyceChain.Models;

public sealed record ChainNodeState(
    string NodeId,
    long LatestBlockNumber,
    string LatestBlockHash,
    DateTimeOffset LastSyncedAt);
