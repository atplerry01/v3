namespace Whycespace.Systems.Downstream.Cwg.Vaults.Transactions;

public interface IVaultTransactionRegistry
{
    void RegisterTransaction(VaultTransactionRegistryRecord record);
    VaultTransactionRegistryRecord? GetTransaction(Guid transactionId);
    IReadOnlyList<VaultTransactionRegistryRecord> GetTransactionsByVault(Guid vaultId);
    IReadOnlyList<VaultTransactionRegistryRecord> GetTransactionsByAccount(Guid accountId);
    IReadOnlyList<VaultTransactionRegistryRecord> GetTransactionsByStatus(string status);
    IReadOnlyList<VaultTransactionRegistryRecord> ListTransactions();
}
