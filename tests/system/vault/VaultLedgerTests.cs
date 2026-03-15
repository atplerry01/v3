namespace Whycespace.VaultLedger.Tests;

using Whycespace.System.Downstream.Economic.Vault.Ledger;

public sealed class VaultLedgerTests
{
    private readonly VaultLedger _ledger = new();

    private static VaultLedgerEntry CreateEntry(
        Guid? transactionId = null,
        Guid? vaultId = null,
        VaultTransactionType transactionType = VaultTransactionType.Contribution,
        decimal amount = 1000m,
        string currency = "USD",
        Guid? referenceId = null,
        string referenceType = "Workflow",
        string metadata = "")
    {
        return new VaultLedgerEntry(
            TransactionId: transactionId ?? Guid.NewGuid(),
            VaultId: vaultId ?? Guid.NewGuid(),
            TransactionType: transactionType,
            Amount: amount,
            Currency: currency,
            ReferenceId: referenceId ?? Guid.NewGuid(),
            ReferenceType: referenceType,
            Timestamp: DateTime.UtcNow,
            Metadata: metadata);
    }

    [Fact]
    public void RecordEntry_SuccessfullyRecords()
    {
        var entry = CreateEntry();

        _ledger.RecordEntry(entry);

        var result = _ledger.GetEntry(entry.TransactionId);
        Assert.NotNull(result);
        Assert.Equal(entry.TransactionId, result.TransactionId);
        Assert.Equal(entry.VaultId, result.VaultId);
        Assert.Equal(entry.Amount, result.Amount);
    }

    [Fact]
    public void RecordEntry_ImmutableAfterRecording()
    {
        var entry = CreateEntry();
        _ledger.RecordEntry(entry);

        var result = _ledger.GetEntry(entry.TransactionId);

        Assert.NotNull(result);
        Assert.Equal(entry.TransactionId, result.TransactionId);
        Assert.Equal(entry.TransactionType, result.TransactionType);
        Assert.Equal(entry.Amount, result.Amount);
        Assert.Equal(entry.Currency, result.Currency);
        Assert.Equal(entry.Timestamp, result.Timestamp);
    }

    [Fact]
    public void GetEntriesByVault_ReturnsMatchingEntries()
    {
        var vaultId = Guid.NewGuid();
        var entry1 = CreateEntry(vaultId: vaultId);
        var entry2 = CreateEntry(vaultId: vaultId);
        var entry3 = CreateEntry(); // different vault

        _ledger.RecordEntry(entry1);
        _ledger.RecordEntry(entry2);
        _ledger.RecordEntry(entry3);

        var results = _ledger.GetEntriesByVault(vaultId);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(vaultId, r.VaultId));
    }

    [Fact]
    public void GetEntriesByVault_NoMatch_ReturnsEmpty()
    {
        var results = _ledger.GetEntriesByVault(Guid.NewGuid());
        Assert.Empty(results);
    }

    [Fact]
    public void GetEntriesByReference_ReturnsMatchingEntries()
    {
        var referenceId = Guid.NewGuid();
        var entry1 = CreateEntry(referenceId: referenceId);
        var entry2 = CreateEntry(referenceId: referenceId);
        var entry3 = CreateEntry(); // different reference

        _ledger.RecordEntry(entry1);
        _ledger.RecordEntry(entry2);
        _ledger.RecordEntry(entry3);

        var results = _ledger.GetEntriesByReference(referenceId);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(referenceId, r.ReferenceId));
    }

    [Fact]
    public void GetEntriesByReference_NoMatch_ReturnsEmpty()
    {
        var results = _ledger.GetEntriesByReference(Guid.NewGuid());
        Assert.Empty(results);
    }

    [Fact]
    public void RecordEntry_DuplicateTransactionId_Throws()
    {
        var transactionId = Guid.NewGuid();
        var entry1 = CreateEntry(transactionId: transactionId);
        var entry2 = CreateEntry(transactionId: transactionId);

        _ledger.RecordEntry(entry1);

        Assert.Throws<InvalidOperationException>(() => _ledger.RecordEntry(entry2));
    }

    [Fact]
    public void GetEntry_NonExistent_ReturnsNull()
    {
        var result = _ledger.GetEntry(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public void ListEntries_ReturnsAllRecorded()
    {
        var entry1 = CreateEntry();
        var entry2 = CreateEntry();

        _ledger.RecordEntry(entry1);
        _ledger.RecordEntry(entry2);

        var results = _ledger.ListEntries();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void ListEntries_EmptyLedger_ReturnsEmpty()
    {
        var results = _ledger.ListEntries();
        Assert.Empty(results);
    }

    [Fact]
    public void RecordEntry_AllTransactionTypes_Succeed()
    {
        foreach (var type in Enum.GetValues<VaultTransactionType>())
        {
            var entry = CreateEntry(transactionType: type);
            _ledger.RecordEntry(entry);

            var result = _ledger.GetEntry(entry.TransactionId);
            Assert.NotNull(result);
            Assert.Equal(type, result.TransactionType);
        }
    }
}
