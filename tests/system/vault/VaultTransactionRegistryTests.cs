namespace Whycespace.VaultTransactionRegistry.Tests;

using Whycespace.Systems.Downstream.Economic.Vault.TransactionRegistry;

public sealed class VaultTransactionRegistryTests
{
    private readonly VaultTransactionRegistry _registry = new();

    private static VaultTransactionRegistryRecord CreateRecord(
        Guid? transactionId = null,
        Guid? vaultId = null,
        Guid? accountId = null,
        string transactionType = "Contribution",
        string status = "Pending",
        decimal amount = 1000m,
        string currency = "USD",
        Guid? initiatorId = null)
    {
        return new VaultTransactionRegistryRecord(
            TransactionId: transactionId ?? Guid.NewGuid(),
            VaultId: vaultId ?? Guid.NewGuid(),
            VaultAccountId: accountId ?? Guid.NewGuid(),
            TransactionType: transactionType,
            TransactionStatus: status,
            Amount: amount,
            Currency: currency,
            InitiatorIdentityId: initiatorId ?? Guid.NewGuid(),
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow);
    }

    [Fact]
    public void RegisterTransaction_SuccessfullyRegisters()
    {
        var record = CreateRecord();

        _registry.RegisterTransaction(record);

        var result = _registry.GetTransaction(record.TransactionId);
        Assert.NotNull(result);
        Assert.Equal(record.TransactionId, result.TransactionId);
        Assert.Equal(record.VaultId, result.VaultId);
        Assert.Equal(record.Amount, result.Amount);
    }

    [Fact]
    public void RegisterTransaction_DuplicateTransaction_Throws()
    {
        var record = CreateRecord();
        _registry.RegisterTransaction(record);

        Assert.Throws<InvalidOperationException>(() => _registry.RegisterTransaction(record));
    }

    [Fact]
    public void GetTransaction_ReturnsCorrectRecord()
    {
        var record = CreateRecord();
        _registry.RegisterTransaction(record);

        var result = _registry.GetTransaction(record.TransactionId);

        Assert.NotNull(result);
        Assert.Equal(record.VaultAccountId, result.VaultAccountId);
        Assert.Equal(record.TransactionType, result.TransactionType);
        Assert.Equal(record.TransactionStatus, result.TransactionStatus);
    }

    [Fact]
    public void GetTransaction_NonExistent_ReturnsNull()
    {
        var result = _registry.GetTransaction(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public void GetTransactionsByVault_ReturnsMatchingTransactions()
    {
        var vaultId = Guid.NewGuid();
        var record1 = CreateRecord(vaultId: vaultId);
        var record2 = CreateRecord(vaultId: vaultId);
        var record3 = CreateRecord(); // different vault

        _registry.RegisterTransaction(record1);
        _registry.RegisterTransaction(record2);
        _registry.RegisterTransaction(record3);

        var results = _registry.GetTransactionsByVault(vaultId);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(vaultId, r.VaultId));
    }

    [Fact]
    public void GetTransactionsByVault_NoMatch_ReturnsEmpty()
    {
        var results = _registry.GetTransactionsByVault(Guid.NewGuid());
        Assert.Empty(results);
    }

    [Fact]
    public void GetTransactionsByAccount_ReturnsMatchingTransactions()
    {
        var accountId = Guid.NewGuid();
        var record1 = CreateRecord(accountId: accountId);
        var record2 = CreateRecord(accountId: accountId);
        var record3 = CreateRecord(); // different account

        _registry.RegisterTransaction(record1);
        _registry.RegisterTransaction(record2);
        _registry.RegisterTransaction(record3);

        var results = _registry.GetTransactionsByAccount(accountId);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(accountId, r.VaultAccountId));
    }

    [Fact]
    public void GetTransactionsByAccount_NoMatch_ReturnsEmpty()
    {
        var results = _registry.GetTransactionsByAccount(Guid.NewGuid());
        Assert.Empty(results);
    }

    [Fact]
    public void GetTransactionsByStatus_ReturnsMatchingTransactions()
    {
        var record1 = CreateRecord(status: "Completed");
        var record2 = CreateRecord(status: "Completed");
        var record3 = CreateRecord(status: "Pending");

        _registry.RegisterTransaction(record1);
        _registry.RegisterTransaction(record2);
        _registry.RegisterTransaction(record3);

        var results = _registry.GetTransactionsByStatus("Completed");

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal("Completed", r.TransactionStatus));
    }

    [Fact]
    public void GetTransactionsByStatus_NoMatch_ReturnsEmpty()
    {
        var results = _registry.GetTransactionsByStatus("Cancelled");
        Assert.Empty(results);
    }

    [Fact]
    public void ListTransactions_ReturnsAllRegistered()
    {
        var record1 = CreateRecord();
        var record2 = CreateRecord();

        _registry.RegisterTransaction(record1);
        _registry.RegisterTransaction(record2);

        var results = _registry.ListTransactions();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void ListTransactions_EmptyRegistry_ReturnsEmpty()
    {
        var results = _registry.ListTransactions();
        Assert.Empty(results);
    }
}
