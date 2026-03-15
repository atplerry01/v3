namespace Whycespace.Domain.Core.Vault;

public sealed record VaultTransactionReference(
    Guid TransactionId,
    DateTimeOffset Timestamp,
    string TransactionType);
