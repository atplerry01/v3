namespace Whycespace.Engines.T3I.Reporting.Economic;

public sealed record RecordVaultEvidenceCommand(
    Guid EvidenceId,
    Guid VaultId,
    Guid TransactionId,
    string EvidenceType,
    DateTime EvidenceTimestamp,
    Guid RequestedBy,
    string ReferenceId = "",
    string ReferenceType = "");
