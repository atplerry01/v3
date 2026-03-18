namespace Whycespace.Engines.T3I.Reporting.Economic.Models;

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
