namespace Whycespace.Engines.T3I.Reporting.Economic;

using global::System.Security.Cryptography;
using global::System.Text;

public sealed class VaultAuditEngine
{
    public VaultAuditResult ExecuteAudit(
        ExecuteVaultAuditCommand command,
        IReadOnlyList<LedgerEntry> ledgerEntries,
        IReadOnlyList<TransactionRecord> transactions)
    {
        var validationError = Validate(command);
        if (validationError is not null)
        {
            return new VaultAuditResult(
                AuditId: command.AuditId,
                VaultId: command.VaultId,
                TransactionCount: 0,
                LedgerEntryCount: 0,
                TotalCredits: 0m,
                TotalDebits: 0m,
                NetBalance: 0m,
                AuditStatus: "Failed",
                AuditSummary: validationError,
                CompletedAt: DateTime.UtcNow);
        }

        var filteredLedger = FilterLedgerEntries(ledgerEntries, command);
        var filteredTransactions = FilterTransactions(transactions, command);

        var totalCredits = filteredLedger
            .Where(e => e.EntryType == "Credit")
            .Sum(e => e.Amount);

        var totalDebits = filteredLedger
            .Where(e => e.EntryType == "Debit")
            .Sum(e => e.Amount);

        var netBalance = totalCredits - totalDebits;

        var auditHash = GenerateAuditHash(
            command.AuditId, command.VaultId, totalCredits, totalDebits, netBalance);

        var summary = BuildAuditSummary(
            command, filteredLedger.Count, filteredTransactions.Count,
            totalCredits, totalDebits, netBalance);

        return new VaultAuditResult(
            AuditId: command.AuditId,
            VaultId: command.VaultId,
            TransactionCount: filteredTransactions.Count,
            LedgerEntryCount: filteredLedger.Count,
            TotalCredits: totalCredits,
            TotalDebits: totalDebits,
            NetBalance: netBalance,
            AuditStatus: "Completed",
            AuditSummary: summary,
            CompletedAt: DateTime.UtcNow,
            AuditHash: auditHash);
    }

    private static IReadOnlyList<LedgerEntry> FilterLedgerEntries(
        IReadOnlyList<LedgerEntry> entries, ExecuteVaultAuditCommand command)
    {
        return entries
            .Where(e => e.VaultId == command.VaultId
                && e.Timestamp >= command.AuditStartTimestamp
                && e.Timestamp <= command.AuditEndTimestamp)
            .ToList();
    }

    private static IReadOnlyList<TransactionRecord> FilterTransactions(
        IReadOnlyList<TransactionRecord> transactions, ExecuteVaultAuditCommand command)
    {
        return transactions
            .Where(t => t.VaultId == command.VaultId
                && t.Timestamp >= command.AuditStartTimestamp
                && t.Timestamp <= command.AuditEndTimestamp)
            .ToList();
    }

    private static string BuildAuditSummary(
        ExecuteVaultAuditCommand command,
        int ledgerEntryCount, int transactionCount,
        decimal totalCredits, decimal totalDebits, decimal netBalance)
    {
        return $"{command.AuditScope} for vault {command.VaultId}: " +
               $"{ledgerEntryCount} ledger entries, {transactionCount} transactions, " +
               $"credits={totalCredits}, debits={totalDebits}, net={netBalance}";
    }

    private static string GenerateAuditHash(
        Guid auditId, Guid vaultId,
        decimal totalCredits, decimal totalDebits, decimal netBalance)
    {
        var input = $"{auditId}|{vaultId}|{totalCredits}|{totalDebits}|{netBalance}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    private static string? Validate(ExecuteVaultAuditCommand command)
    {
        if (command.AuditId == Guid.Empty)
            return "AuditId must not be empty";

        if (command.VaultId == Guid.Empty)
            return "VaultId must not be empty";

        if (command.RequestedBy == Guid.Empty)
            return "RequestedBy must not be empty";

        if (command.AuditEndTimestamp <= command.AuditStartTimestamp)
            return "AuditEndTimestamp must be after AuditStartTimestamp";

        if (!Enum.IsDefined(command.AuditScope))
            return $"Invalid audit scope: {command.AuditScope}";

        return null;
    }
}

public sealed record LedgerEntry(
    Guid EntryId,
    Guid VaultId,
    string EntryType,
    decimal Amount,
    DateTime Timestamp);

public sealed record TransactionRecord(
    Guid TransactionId,
    Guid VaultId,
    string TransactionType,
    decimal Amount,
    DateTime Timestamp);
