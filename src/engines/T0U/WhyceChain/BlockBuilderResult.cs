namespace Whycespace.Engines.T0U.WhyceChain;

using Whycespace.Systems.Upstream.WhyceChain.Ledger;

public sealed record BlockBuilderResult(
    ChainBlock Block,
    string MerkleRoot,
    string BlockHash,
    int EntryCount,
    DateTime GeneratedAt,
    string TraceId);
