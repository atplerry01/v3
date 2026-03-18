namespace Whycespace.Engines.T0U.WhyceChain.Block.Builder;

using Whycespace.Systems.Upstream.WhyceChain.Ledger;

public sealed record BlockBuilderResult(
    ChainBlock Block,
    string MerkleRoot,
    string BlockHash,
    int EntryCount,
    DateTime GeneratedAt,
    string TraceId);
