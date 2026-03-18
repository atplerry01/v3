namespace Whycespace.Engines.T2E.Economic.Vault.Models;

public sealed record VaultFreezeResult(
    Guid VaultId,
    Guid VaultAccountId,
    bool IsFrozen,
    string FreezeScope,
    string FreezeReason,
    DateTime EvaluatedAt);
