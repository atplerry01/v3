namespace Whycespace.Systems.Downstream.Cwg.Vaults.Ledger;

public sealed record VaultLedgerEntry(
    Guid TransactionId,
    Guid VaultId,
    VaultTransactionType TransactionType,
    decimal Amount,
    string Currency,
    Guid ReferenceId,
    string ReferenceType,
    DateTime Timestamp,
    string Metadata = "");
