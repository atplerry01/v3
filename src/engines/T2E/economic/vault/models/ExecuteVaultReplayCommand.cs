namespace Whycespace.Engines.T2E.Economic.Vault.Models;

public sealed record ExecuteVaultReplayCommand(
    Guid ReplayId,
    Guid VaultId,
    Guid SnapshotId,
    DateTime ReplayStartTimestamp,
    DateTime ReplayEndTimestamp,
    Guid RequestedBy,
    string? ReplayScope = null,
    string? ReferenceId = null);