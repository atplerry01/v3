namespace Whycespace.System.Upstream.WhyceChain.Models;

public sealed record IntegrityVerificationResult(
    bool LedgerValid,
    bool BlockChainValid,
    bool MerkleRootValid,
    bool MerkleProofValid,
    IReadOnlyList<long> TamperedEntries,
    DateTimeOffset VerificationTimestamp,
    string TraceId);
