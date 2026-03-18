namespace Whycespace.Engines.T2E.Economic.Vault.Freeze.Models;

public sealed record ExecuteVaultUnfreezeCommand(
    Guid VaultId,
    Guid VaultAccountId,
    Guid RequestedBy,
    string UnfreezeReason,
    DateTime RequestedAt);
