namespace Whycespace.System.Downstream.Economic.Vault.TransactionRegistry;

public interface IVaultTransactionRegistry
{
    void RegisterTransaction(VaultTransactionRegistryRecord record);
    VaultTransactionRegistryRecord? GetTransaction(Guid transactionId);
    IReadOnlyList<VaultTransactionRegistryRecord> GetTransactionsByVault(Guid vaultId);
    IReadOnlyList<VaultTransactionRegistryRecord> GetTransactionsByAccount(Guid accountId);
    IReadOnlyList<VaultTransactionRegistryRecord> GetTransactionsByStatus(string status);
    IReadOnlyList<VaultTransactionRegistryRecord> ListTransactions();
}
