namespace Whycespace.Engines.T2E.Economic.Vault.Reconciliation.Models;

public sealed record VaultReconciliationResult(
    Guid ReconciliationId,
    Guid VaultId,
    bool IsBalanced,
    decimal TotalCredits,
    decimal TotalDebits,
    decimal LedgerBalance,
    decimal ComputedBalance,
    string ReconciliationStatus,
    string ReconciliationNotes,
    DateTime CompletedAt);
