namespace Whycespace.Engines.T2E.Economic.Vault.Models;

public sealed record VaultRecoveryResult(
    Guid RecoveryId,
    Guid VaultId,
    Guid SnapshotId,
    decimal RecoveredVaultBalance,
    int RecoveredTransactionCount,
    int RecoveredParticipantCount,
    string RecoveryStatus,
    DateTime CompletedAt,
    string? RecoveryNotes = null);
