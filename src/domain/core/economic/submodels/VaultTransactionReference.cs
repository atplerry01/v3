namespace Whycespace.Domain.Core.Economic;

public sealed record VaultTransactionReference(
    Guid TransactionId,
    DateTimeOffset Timestamp,
    string TransactionType);
