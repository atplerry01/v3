namespace Whycespace.Systems.Downstream.Economic.Vault.TransactionRegistry;

public sealed class VaultTransactionRegistry : IVaultTransactionRegistry
{
    private readonly Dictionary<Guid, VaultTransactionRegistryRecord> _transactions = new();
    private readonly Dictionary<Guid, List<Guid>> _vaultIndex = new();
    private readonly Dictionary<Guid, List<Guid>> _accountIndex = new();
    private readonly Dictionary<string, List<Guid>> _statusIndex = new();

    public void RegisterTransaction(VaultTransactionRegistryRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.VaultId == Guid.Empty)
            throw new ArgumentException("VaultId is required.", nameof(record));

        if (record.VaultAccountId == Guid.Empty)
            throw new ArgumentException("VaultAccountId is required.", nameof(record));

        if (_transactions.ContainsKey(record.TransactionId))
            throw new InvalidOperationException($"Transaction '{record.TransactionId}' is already registered.");

        _transactions[record.TransactionId] = record;

        if (!_vaultIndex.TryGetValue(record.VaultId, out var vaultList))
        {
            vaultList = new List<Guid>();
            _vaultIndex[record.VaultId] = vaultList;
        }
        vaultList.Add(record.TransactionId);

        if (!_accountIndex.TryGetValue(record.VaultAccountId, out var accountList))
        {
            accountList = new List<Guid>();
            _accountIndex[record.VaultAccountId] = accountList;
        }
        accountList.Add(record.TransactionId);

        if (!_statusIndex.TryGetValue(record.TransactionStatus, out var statusList))
        {
            statusList = new List<Guid>();
            _statusIndex[record.TransactionStatus] = statusList;
        }
        statusList.Add(record.TransactionId);
    }

    public VaultTransactionRegistryRecord? GetTransaction(Guid transactionId)
    {
        _transactions.TryGetValue(transactionId, out var record);
        return record;
    }

    public IReadOnlyList<VaultTransactionRegistryRecord> GetTransactionsByVault(Guid vaultId)
    {
        if (!_vaultIndex.TryGetValue(vaultId, out var ids))
            return [];

        return ids.Select(id => _transactions[id]).ToList();
    }

    public IReadOnlyList<VaultTransactionRegistryRecord> GetTransactionsByAccount(Guid accountId)
    {
        if (!_accountIndex.TryGetValue(accountId, out var ids))
            return [];

        return ids.Select(id => _transactions[id]).ToList();
    }

    public IReadOnlyList<VaultTransactionRegistryRecord> GetTransactionsByStatus(string status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        if (!_statusIndex.TryGetValue(status, out var ids))
            return [];

        return ids.Select(id => _transactions[id]).ToList();
    }

    public IReadOnlyList<VaultTransactionRegistryRecord> ListTransactions() => _transactions.Values.ToList();
}
