namespace Whycespace.Engines.T2E.Economic.Vault.Adapters;

public sealed record VaultEvidenceAnchorResult(
    Guid AnchorRequestId,
    Guid EvidenceId,
    Guid VaultId,
    string EvidenceHash,
    string ChainTransactionId,
    string AnchorStatus,
    DateTime AnchoredAt,
    string? AnchorSummary = null);
