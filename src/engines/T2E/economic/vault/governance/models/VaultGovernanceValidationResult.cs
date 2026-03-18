namespace Whycespace.Engines.T2E.Economic.Vault.Governance.Models;

public sealed record VaultGovernanceValidationResult(
    Guid VaultId,
    string OperationType,
    bool IsApproved,
    string GovernanceDecision,
    string GovernanceReason,
    DateTime EvaluatedAt);
