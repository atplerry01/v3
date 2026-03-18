namespace Whycespace.Engines.T2E.Economic.Vault.Models;

public sealed record ExecuteVaultFreezeCommand(
    Guid VaultId,
    Guid VaultAccountId,
    Guid RequestedBy,
    string FreezeReason,
    string FreezeScope,
    DateTime RequestedAt);
