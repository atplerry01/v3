namespace Whycespace.ProjectionRuntime.Projections.Core.Economics.Vault;

public sealed record VaultTransactionReadModel(
    Guid TransactionId,
    Guid VaultId,
    Guid ParticipantId,
    string TransactionType,
    decimal Amount,
    string Currency,
    string TransactionStatus,
    DateTime TransactionTimestamp,
    DateTime RecordedAt,
    string? TransactionReference = null,
    string? TransactionSummary = null);
