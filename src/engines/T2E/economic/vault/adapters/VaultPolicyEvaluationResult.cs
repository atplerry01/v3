namespace Whycespace.Engines.T2E.Economic.Vault.Adapters;

public sealed record VaultPolicyEvaluationResult(
    Guid VaultId,
    string OperationType,
    bool IsAllowed,
    string PolicyDecision,
    string PolicyReason,
    DateTime EvaluatedAt);
