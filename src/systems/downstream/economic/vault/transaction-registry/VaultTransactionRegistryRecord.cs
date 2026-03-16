namespace Whycespace.Systems.Downstream.Economic.Vault.TransactionRegistry;

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
