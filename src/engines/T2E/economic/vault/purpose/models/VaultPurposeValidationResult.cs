namespace Whycespace.Engines.T2E.Economic.Vault.Models;

public sealed record VaultPurposeValidationResult(
    Guid VaultId,
    string VaultPurpose,
    string TransactionType,
    bool IsAllowed,
    string ValidationReason,
    DateTime EvaluatedAt);
