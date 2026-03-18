namespace Whycespace.Engines.T2E.Economic.Vault.Models;

public sealed record ExecuteVaultUnfreezeCommand(
    Guid VaultId,
    Guid VaultAccountId,
    Guid RequestedBy,
    string UnfreezeReason,
    DateTime RequestedAt);
