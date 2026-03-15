namespace Whycespace.Tests.Engines.Vault;

using Whycespace.Engines.T3I.Economic.Vault;
using Xunit;

public sealed class VaultAuditEngineTests
{
    private readonly VaultAuditEngine _engine = new();

    private static readonly Guid VaultId = Guid.NewGuid();
    private static readonly Guid RequestedBy = Guid.NewGuid();
    private static readonly DateTime Start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc);

    private static ExecuteVaultAuditCommand CreateCommand(
        VaultAuditScope scope = VaultAuditScope.FullVaultAudit,
        Guid? auditId = null,
        Guid? vaultId = null,
        Guid? requestedBy = null,
        DateTime? start = null,
        DateTime? end = null)
    {
        return new ExecuteVaultAuditCommand(
            AuditId: auditId ?? Guid.NewGuid(),
            VaultId: vaultId ?? VaultId,
            AuditStartTimestamp: start ?? Start,
            AuditEndTimestamp: end ?? End,
            AuditScope: scope,
            RequestedBy: requestedBy ?? RequestedBy);
    }

    [Fact]
    public void AuditSuccessTest_ReportGeneratedSuccessfully()
    {
        var command = CreateCommand();
        var ledger = new List<LedgerEntry>
        {
            new(Guid.NewGuid(), VaultId, "Credit", 500m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Debit", 200m, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc))
        };
        var transactions = new List<TransactionRecord>
        {
            new(Guid.NewGuid(), VaultId, "Contribution", 500m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Withdrawal", 200m, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc))
        };

        var result = _engine.ExecuteAudit(command, ledger, transactions);

        Assert.Equal("Completed", result.AuditStatus);
        Assert.Equal(command.AuditId, result.AuditId);
        Assert.Equal(VaultId, result.VaultId);
        Assert.NotEmpty(result.AuditHash);
        Assert.NotEmpty(result.AuditSummary);
    }

    [Fact]
    public void LedgerAggregationTest_CreditAndDebitTotalsCorrect()
    {
        var command = CreateCommand(VaultAuditScope.LedgerAudit);
        var ledger = new List<LedgerEntry>
        {
            new(Guid.NewGuid(), VaultId, "Credit", 1000m, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Credit", 2500m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Debit", 750m, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Debit", 250m, new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc))
        };

        var result = _engine.ExecuteAudit(command, ledger, Array.Empty<TransactionRecord>());

        Assert.Equal(3500m, result.TotalCredits);
        Assert.Equal(1000m, result.TotalDebits);
        Assert.Equal(4, result.LedgerEntryCount);
    }

    [Fact]
    public void TransactionCountTest_CountsCorrectly()
    {
        var command = CreateCommand(VaultAuditScope.TransactionAudit);
        var transactions = new List<TransactionRecord>
        {
            new(Guid.NewGuid(), VaultId, "Contribution", 100m, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Transfer", 200m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Withdrawal", 50m, new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc))
        };

        var result = _engine.ExecuteAudit(command, Array.Empty<LedgerEntry>(), transactions);

        Assert.Equal(3, result.TransactionCount);
    }

    [Fact]
    public void BalanceValidationTest_NetBalanceComputedCorrectly()
    {
        var command = CreateCommand(VaultAuditScope.BalanceAudit);
        var ledger = new List<LedgerEntry>
        {
            new(Guid.NewGuid(), VaultId, "Credit", 5000m, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Debit", 1500m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Debit", 500m, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc))
        };

        var result = _engine.ExecuteAudit(command, ledger, Array.Empty<TransactionRecord>());

        Assert.Equal(5000m, result.TotalCredits);
        Assert.Equal(2000m, result.TotalDebits);
        Assert.Equal(3000m, result.NetBalance);
    }

    [Fact]
    public void DeterministicAuditTest_SameInputProducesSameOutput()
    {
        var auditId = Guid.NewGuid();
        var command = CreateCommand(auditId: auditId);
        var ledger = new List<LedgerEntry>
        {
            new(Guid.NewGuid(), VaultId, "Credit", 1000m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Debit", 300m, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc))
        };
        var transactions = new List<TransactionRecord>
        {
            new(Guid.NewGuid(), VaultId, "Contribution", 1000m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc))
        };

        var result1 = _engine.ExecuteAudit(command, ledger, transactions);
        var result2 = _engine.ExecuteAudit(command, ledger, transactions);

        Assert.Equal(result1.AuditId, result2.AuditId);
        Assert.Equal(result1.TotalCredits, result2.TotalCredits);
        Assert.Equal(result1.TotalDebits, result2.TotalDebits);
        Assert.Equal(result1.NetBalance, result2.NetBalance);
        Assert.Equal(result1.TransactionCount, result2.TransactionCount);
        Assert.Equal(result1.LedgerEntryCount, result2.LedgerEntryCount);
        Assert.Equal(result1.AuditHash, result2.AuditHash);
    }

    [Fact]
    public void EmptyAuditId_Fails()
    {
        var command = CreateCommand(auditId: Guid.Empty);

        var result = _engine.ExecuteAudit(command, Array.Empty<LedgerEntry>(), Array.Empty<TransactionRecord>());

        Assert.Equal("Failed", result.AuditStatus);
        Assert.Contains("AuditId", result.AuditSummary);
    }

    [Fact]
    public void EmptyVaultId_Fails()
    {
        var command = CreateCommand(vaultId: Guid.Empty);

        var result = _engine.ExecuteAudit(command, Array.Empty<LedgerEntry>(), Array.Empty<TransactionRecord>());

        Assert.Equal("Failed", result.AuditStatus);
        Assert.Contains("VaultId", result.AuditSummary);
    }

    [Fact]
    public void EndBeforeStart_Fails()
    {
        var command = CreateCommand(
            start: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            end: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = _engine.ExecuteAudit(command, Array.Empty<LedgerEntry>(), Array.Empty<TransactionRecord>());

        Assert.Equal("Failed", result.AuditStatus);
        Assert.Contains("AuditEndTimestamp", result.AuditSummary);
    }

    [Fact]
    public void EntriesOutsideWindow_AreExcluded()
    {
        var command = CreateCommand();
        var ledger = new List<LedgerEntry>
        {
            new(Guid.NewGuid(), VaultId, "Credit", 1000m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Credit", 9999m, new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Credit", 9999m, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc))
        };

        var result = _engine.ExecuteAudit(command, ledger, Array.Empty<TransactionRecord>());

        Assert.Equal(1000m, result.TotalCredits);
        Assert.Equal(1, result.LedgerEntryCount);
    }

    [Fact]
    public void DifferentVaultEntries_AreExcluded()
    {
        var otherVault = Guid.NewGuid();
        var command = CreateCommand();
        var ledger = new List<LedgerEntry>
        {
            new(Guid.NewGuid(), VaultId, "Credit", 500m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), otherVault, "Credit", 9999m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc))
        };

        var result = _engine.ExecuteAudit(command, ledger, Array.Empty<TransactionRecord>());

        Assert.Equal(500m, result.TotalCredits);
        Assert.Equal(1, result.LedgerEntryCount);
    }
}
