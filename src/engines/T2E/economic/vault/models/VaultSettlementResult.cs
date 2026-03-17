namespace Whycespace.Engines.T2E.Economic.Vault.Models;

public sealed record VaultSettlementResult(
    Guid SettlementId,
    Guid TransactionId,
    Guid VaultId,
    decimal Amount,
    string Currency,
    bool IsSettled,
    string SettlementStatus,
    DateTime SettledAt);
