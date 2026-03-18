namespace Whycespace.Engines.T3I.Reporting.Economic.Models;

public sealed record VaultEvidenceRecord(
    Guid EvidenceId,
    Guid VaultId,
    Guid TransactionId,
    string EvidenceType,
    string EvidencePayload,
    DateTime EvidenceTimestamp,
    string EvidenceHashCandidate,
    DateTime RecordedAt,
    string EvidenceSummary = "");
