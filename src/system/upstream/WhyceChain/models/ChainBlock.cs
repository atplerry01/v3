namespace Whycespace.System.Upstream.WhyceChain.Models;

public sealed record ChainBlock(
    string BlockId,
    long BlockNumber,
    string PreviousBlockHash,
    string BlockHash,
    string MerkleRoot,
    DateTimeOffset Timestamp,
    IReadOnlyList<string> EntryIds);
