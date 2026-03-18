namespace Whycespace.Engines.T2E.Economic.Vault.Models;

public sealed record VaultReplayResult(
    Guid ReplayId,
    Guid VaultId,
    Guid SnapshotId,
    decimal FinalVaultBalance,
    int ReplayedTransactionCount,
    int ReplayedLedgerEntryCount,
    string ReplayStatus,
    DateTime CompletedAt,
    string? ReplayNotes = null);