namespace Whycespace.Engines.T3I.Reporting.Economic.Models;

public sealed record VaultAuditInput(
    ExecuteVaultAuditCommand Command,
    IReadOnlyList<LedgerEntry> LedgerEntries,
    IReadOnlyList<TransactionRecord> Transactions);
