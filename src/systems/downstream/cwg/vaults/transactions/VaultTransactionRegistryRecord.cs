namespace Whycespace.Systems.Downstream.Cwg.Vaults.Transactions;

public sealed record VaultTransactionRegistryRecord(
    Guid TransactionId,
    Guid VaultId,
    Guid VaultAccountId,
    string TransactionType,
    string TransactionStatus,
    decimal Amount,
    string Currency,
    Guid InitiatorIdentityId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? ReferenceId = null,
    string? ReferenceType = null,
    string? Description = null);
