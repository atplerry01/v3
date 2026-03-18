namespace Whycespace.Engines.T2E.Economic.Vault.Risk.Models;

public sealed record EvaluateVaultRiskCommand(
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
