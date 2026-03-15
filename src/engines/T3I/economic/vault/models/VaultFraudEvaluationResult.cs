namespace Whycespace.Engines.T3I.Economic.Vault.Models;

public sealed record VaultFraudEvaluationResult(
    Guid VaultId,
    Guid TransactionId,
    double FraudScore,
    string FraudRiskLevel,
    bool FraudAlertTriggered,
    string FraudReason,
    DateTime EvaluatedAt);

public enum FraudRiskLevel { Low, Suspicious, HighFraudRisk }
