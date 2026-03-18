namespace Whycespace.Engines.T2E.Economic.Vault.Settlement.Models;

public sealed record ExecuteVaultSettlementCommand(
    Guid SettlementId,
    Guid TransactionId,
    Guid VaultId,
    Guid VaultAccountId,
    decimal Amount,
    string Currency,
    DateTime RequestedAt,
    Guid RequestedBy,
    string? SettlementReference = null,
    string? SettlementScope = null);
