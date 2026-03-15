namespace Whycespace.Engines.T2E.Economic.Vault.Adapters;

public sealed record VaultPolicyEvaluationCommand(
    Guid VaultId,
    Guid VaultAccountId,
    Guid InitiatorIdentityId,
    string OperationType,
    decimal Amount,
    string Currency,
    string VaultPurpose,
    DateTime RequestedAt,
    string? ReferenceId = null,
    string? ReferenceType = null);
