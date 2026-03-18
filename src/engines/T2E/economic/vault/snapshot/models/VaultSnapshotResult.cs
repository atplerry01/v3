namespace Whycespace.Engines.T2E.Economic.Vault.Snapshot.Models;

public sealed record VaultSnapshotResult(
    Guid SnapshotId,
    Guid VaultId,
    decimal VaultBalance,
    int TransactionCount,
    int ParticipantCount,
    string SnapshotStatus,
    DateTime CreatedAt,
    string? SnapshotHash = null);
