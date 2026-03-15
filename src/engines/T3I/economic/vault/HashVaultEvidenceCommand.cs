namespace Whycespace.Engines.T3I.Economic.Vault;

public sealed record HashVaultEvidenceCommand(
    Guid HashId,
    Guid EvidenceId,
    Guid VaultId,
    string EvidencePayload,
    DateTime EvidenceTimestamp,
    Guid RequestedBy,
    string ReferenceId = "",
    string ReferenceType = "");
