namespace Whycespace.Engines.T2E.Economic.Vault.Snapshot.Models;

public sealed record CreateVaultSnapshotCommand(
    Guid SnapshotId,
    Guid VaultId,
    DateTime SnapshotTimestamp,
    Guid RequestedBy,
    string? SnapshotScope = null,
    string? ReferenceId = null);
