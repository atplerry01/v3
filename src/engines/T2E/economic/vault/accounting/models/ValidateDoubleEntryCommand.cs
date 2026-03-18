namespace Whycespace.Engines.T2E.Economic.Vault.Accounting.Models;

public sealed record LedgerEntry(
    Guid AccountId,
    decimal Amount,
    string Currency,
    string Direction);

public sealed record ValidateDoubleEntryCommand(
    Guid TransactionId,
    Guid VaultId,
    string TransactionType,
    List<LedgerEntry> LedgerEntries,
    DateTime RequestedAt);
