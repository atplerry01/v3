namespace Whycespace.Engines.T3I.Monitoring.Economic.Models;

public sealed record EvaluateVaultFraudCommand(
    Guid VaultId,
    Guid VaultAccountId,
    Guid TransactionId,
    Guid InitiatorIdentityId,
    string OperationType,
    decimal Amount,
    string Currency,
    DateTime RequestedAt,
    string? ReferenceId = null,
    string? ReferenceType = null);
