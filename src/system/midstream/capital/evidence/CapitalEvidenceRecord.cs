namespace Whycespace.System.Midstream.Capital.Evidence;

public sealed record CapitalEvidenceRecord(
    Guid EvidenceId,
    CapitalEvidenceOperationType OperationType,
    Guid CapitalId,
    Guid PoolId,
    Guid ReferenceId,
    decimal Amount,
    string Currency,
    string EvidenceHash,
    Guid LedgerEntryId,
    DateTime CreatedAt,
    Guid TraceId,
    Guid CorrelationId);
