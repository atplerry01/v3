namespace Whycespace.Domain.Economic.Vault;

public sealed record VaultTransactionReference(
    Guid TransactionId,
    DateTimeOffset Timestamp,
    string TransactionType);
