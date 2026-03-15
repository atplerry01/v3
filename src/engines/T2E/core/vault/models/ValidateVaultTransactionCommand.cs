namespace Whycespace.Engines.T2E.Core.Vault.Models;

public sealed record ValidateVaultTransactionCommand(
    Guid TransactionId,
    Guid VaultId,
    Guid VaultAccountId,
    Guid InitiatorIdentityId,
    string TransactionType,
    decimal Amount,
    string Currency,
    DateTime RequestedAt,
    string? ReferenceId = null,
    string? ReferenceType = null);
