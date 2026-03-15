namespace Whycespace.Engines.T2E.Economic.Vault.Adapters;

public sealed record AnchorVaultEvidenceCommand(
    Guid AnchorRequestId,
    Guid EvidenceId,
    Guid VaultId,
    string EvidenceHash,
    DateTime EvidenceTimestamp,
    Guid RequestedBy,
    string? ReferenceId = null,
    string? ReferenceType = null);
