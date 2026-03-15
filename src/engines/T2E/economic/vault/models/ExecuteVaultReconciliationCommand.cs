namespace Whycespace.Engines.T2E.Economic.Vault.Models;

public sealed record ExecuteVaultReconciliationCommand(
    Guid ReconciliationId,
    Guid VaultId,
    string ReconciliationScope,
    DateTime RequestedAt,
    Guid RequestedBy,
    string? ReferenceId = null,
    string? ReferenceType = null);
