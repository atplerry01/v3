namespace Whycespace.Engines.T2E.Economic.Vault.Models;

public sealed record VaultTransactionValidationResult(
    Guid TransactionId,
    Guid VaultId,
    bool IsValid,
    string ValidationStatus,
    string ValidationReason,
    DateTime EvaluatedAt);
