namespace Whycespace.Systems.Downstream.Cwg.Vaults.Ledger;

public interface IVaultLedger
{
    void RecordEntry(VaultLedgerEntry entry);
    VaultLedgerEntry? GetEntry(Guid transactionId);
    IReadOnlyList<VaultLedgerEntry> GetEntriesByVault(Guid vaultId);
    IReadOnlyList<VaultLedgerEntry> GetEntriesByReference(Guid referenceId);
    IReadOnlyList<VaultLedgerEntry> ListEntries();
}
