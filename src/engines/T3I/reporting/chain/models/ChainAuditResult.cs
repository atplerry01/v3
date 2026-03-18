namespace Whycespace.Engines.T3I.Reporting.Chain.Models;

public sealed record ChainAuditResult(
    int TotalBlocks,
    int TotalEntries,
    int BrokenBlockLinks,
    int InvalidBlockHashes,
    int MerkleRootMismatches,
    int SequenceGaps,
    bool AnomalyDetected,
    DateTime AuditTimestamp,
    string TraceId);
