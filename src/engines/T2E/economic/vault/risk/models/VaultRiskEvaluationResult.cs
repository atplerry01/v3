namespace Whycespace.Engines.T2E.Economic.Vault.Models;

public sealed record VaultRiskEvaluationResult(
    Guid VaultId,
    Guid TransactionId,
    decimal RiskScore,
    string RiskLevel,
    bool IsAllowed,
    string RiskDecision,
    string RiskReason,
    DateTime EvaluatedAt);
