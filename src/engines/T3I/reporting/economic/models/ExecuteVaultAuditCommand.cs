namespace Whycespace.Engines.T3I.Reporting.Economic.Models;

public enum VaultAuditScope
{
    LedgerAudit,
    TransactionAudit,
    BalanceAudit,
    FullVaultAudit
}

public sealed record ExecuteVaultAuditCommand(
    Guid AuditId,
    Guid VaultId,
    DateTime AuditStartTimestamp,
    DateTime AuditEndTimestamp,
    VaultAuditScope AuditScope,
    Guid RequestedBy,
    string ReferenceId = "",
    string ReferenceType = "");