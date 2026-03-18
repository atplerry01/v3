namespace Whycespace.Engines.T2E.Economic.Vault.Models;

public sealed record ExecuteVaultRecoveryCommand(
    Guid RecoveryId,
    Guid VaultId,
    Guid SnapshotId,
    DateTime RequestedAt,
    Guid RequestedBy,
    string? RecoveryScope = null,
    string? ReferenceId = null);
