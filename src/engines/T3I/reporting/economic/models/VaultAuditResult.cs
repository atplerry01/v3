namespace Whycespace.Engines.T3I.Reporting.Economic.Models;

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