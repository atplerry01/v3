namespace Whycespace.System.Upstream.WhyceChain.Ledger;

/// <summary>
/// Canonical immutable ledger entry for WhyceChain evidence logging.
/// Records policy decisions, identity verifications, governance actions,
/// workflow outcomes, economic transaction evidence, and audit records.
/// EntryHash is generated from deterministic hashing of all constituent fields.
/// </summary>
public sealed record ChainLedgerEntry(
    Guid EntryId,
    string EntryType,
    string AggregateId,
    long SequenceNumber,
    string PayloadHash,
    string MetadataHash,
    string PreviousEntryHash,
    string EntryHash,
    DateTimeOffset Timestamp,
    string TraceId,
    string CorrelationId,
    int EventVersion);
