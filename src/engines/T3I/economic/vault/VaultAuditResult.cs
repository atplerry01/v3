namespace Whycespace.Engines.T3I.Economic.Vault;

public sealed record VaultAuditResult(
    Guid AuditId,
    Guid VaultId,
    int TransactionCount,
    int LedgerEntryCount,
    decimal TotalCredits,
    decimal TotalDebits,
    decimal NetBalance,
    string AuditStatus,
    string AuditSummary,
    DateTime CompletedAt,
    string AuditHash = "");