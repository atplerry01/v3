namespace Whycespace.Tests.Engines.Vault;

using Whycespace.Engines.T3I.Atlas.Economic.Engines;
using Whycespace.Engines.T3I.Atlas.Economic.Models;
using Xunit;

public sealed class VaultBalanceAnalyticsEngineTests
{
    private readonly VaultBalanceAnalyticsEngine _engine = new();

    private static readonly Guid VaultId = Guid.NewGuid();
    private static readonly Guid RequestedBy = Guid.NewGuid();
    private static readonly DateTime Start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc);

    private static ExecuteVaultBalanceAnalyticsCommand CreateCommand(
        AnalysisScope scope = AnalysisScope.BalanceTrend,
        Guid? analyticsId = null,
        Guid? vaultId = null,
        Guid? requestedBy = null,
        DateTime? start = null,
        DateTime? end = null)
    {
        return new ExecuteVaultBalanceAnalyticsCommand(
            AnalyticsId: analyticsId ?? Guid.NewGuid(),
            VaultId: vaultId ?? VaultId,
            AnalysisStartTimestamp: start ?? Start,
            AnalysisEndTimestamp: end ?? End,
            AnalysisScope: scope,
            RequestedBy: requestedBy ?? RequestedBy);
    }

    [Fact]
    public void BalanceTrendAnalysisTest_IncreasingTrendDetected()
    {
        var command = CreateCommand();
        var ledger = new List<LedgerEntry>
        {
            new(Guid.NewGuid(), VaultId, "Credit", 1000m, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Credit", 500m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Credit", 300m, new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc))
        };

        var result = _engine.ExecuteAnalytics(command, ledger);

        Assert.Equal("Increasing", result.BalanceTrend);
    }

    [Fact]
    public void BalanceTrendAnalysisTest_DecreasingTrendDetected()
    {
        var command = CreateCommand();
        var ledger = new List<LedgerEntry>
        {
            new(Guid.NewGuid(), VaultId, "Credit", 1000m, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Debit", 800m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Debit", 300m, new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc))
        };

        var result = _engine.ExecuteAnalytics(command, ledger);

        Assert.Equal("Decreasing", result.BalanceTrend);
    }

    [Fact]
    public void BalanceTrendAnalysisTest_StableTrendForSingleEntry()
    {
        var command = CreateCommand();
        var ledger = new List<LedgerEntry>
        {
            new(Guid.NewGuid(), VaultId, "Credit", 500m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc))
        };

        var result = _engine.ExecuteAnalytics(command, ledger);

        Assert.Equal("Stable", result.BalanceTrend);
    }

    [Fact]
    public void AverageBalanceTest_CalculatedCorrectly()
    {
        var command = CreateCommand(AnalysisScope.LiquidityAnalysis);
        var ledger = new List<LedgerEntry>
        {
            new(Guid.NewGuid(), VaultId, "Credit", 1000m, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Credit", 2000m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Debit", 500m, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc))
        };

        var result = _engine.ExecuteAnalytics(command, ledger);

        // Running balances: 1000, 3000, 2500
        // Average: (1000 + 3000 + 2500) / 3 = 2166.6667
        var expectedAverage = (1000m + 3000m + 2500m) / 3m;
        Assert.Equal(expectedAverage, result.AverageBalance);
    }

    [Fact]
    public void MinMaxBalanceTest_CalculatedCorrectly()
    {
        var command = CreateCommand();
        var ledger = new List<LedgerEntry>
        {
            new(Guid.NewGuid(), VaultId, "Credit", 5000m, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Debit", 3000m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Credit", 1000m, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Debit", 500m, new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc))
        };

        var result = _engine.ExecuteAnalytics(command, ledger);

        // Running balances: 5000, 2000, 3000, 2500
        Assert.Equal(5000m, result.MaximumBalance);
        Assert.Equal(2000m, result.MinimumBalance);
    }

    [Fact]
    public void GrowthRateTest_ComputedCorrectly()
    {
        var command = CreateCommand();
        var ledger = new List<LedgerEntry>
        {
            new(Guid.NewGuid(), VaultId, "Credit", 1000m, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Credit", 500m, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc))
        };

        var result = _engine.ExecuteAnalytics(command, ledger);

        // Running balances: 1000, 1500
        // Growth rate: (1500 - 1000) / |1000| * 100 = 50%
        Assert.Equal(50m, result.BalanceGrowthRate);
    }

    [Fact]
    public void GrowthRateTest_NegativeGrowth()
    {
        var command = CreateCommand();
        var ledger = new List<LedgerEntry>
        {
            new(Guid.NewGuid(), VaultId, "Credit", 2000m, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Debit", 1000m, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc))
        };

        var result = _engine.ExecuteAnalytics(command, ledger);

        // Running balances: 2000, 1000
        // Growth rate: (1000 - 2000) / |2000| * 100 = -50%
        Assert.Equal(-50m, result.BalanceGrowthRate);
    }

    [Fact]
    public void DeterministicAnalyticsTest_SameInputProducesSameOutput()
    {
        var analyticsId = Guid.NewGuid();
        var command = CreateCommand(analyticsId: analyticsId);
        var ledger = new List<LedgerEntry>
        {
            new(Guid.NewGuid(), VaultId, "Credit", 1000m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "Debit", 300m, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc))
        };

        var result1 = _engine.ExecuteAnalytics(command, ledger);
        var result2 = _engine.ExecuteAnalytics(command, ledger);

        Assert.Equal(result1.AnalyticsId, result2.AnalyticsId);
        Assert.Equal(result1.CurrentBalance, result2.CurrentBalance);
        Assert.Equal(result1.AverageBalance, result2.AverageBalance);
        Assert.Equal(result1.MinimumBalance, result2.MinimumBalance);
        Assert.Equal(result1.MaximumBalance, result2.MaximumBalance);
        Assert.Equal(result1.BalanceGrowthRate, result2.BalanceGrowthRate);
        Assert.Equal(result1.BalanceTrend, result2.BalanceTrend);
        Assert.Equal(result1.AnalyticsHash, result2.AnalyticsHash);
    }

    [Fact]
    public void EmptyAnalyticsId_Fails()
    {
        var command = CreateCommand(analyticsId: Guid.Empty);

        var result = _engine.ExecuteAnalytics(command, Array.Empty<LedgerEntry>());

        Assert.Contains("AnalyticsId", result.AnalyticsSummary);
        Assert.Equal("Unknown", result.BalanceTrend);
    }

    [Fact]
    public void EmptyVaultId_Fails()
    {
        var command = CreateCommand(vaultId: Guid.Empty);

        var result = _engine.ExecuteAnalytics(command, Array.Empty<LedgerEntry>());

        Assert.Contains("VaultId", result.AnalyticsSummary);
    }

    [Fact]
    public void EndBeforeStart_Fails()
    {
        var command = CreateCommand(
            start: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            end: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = _engine.ExecuteAnalytics(command, Array.Empty<LedgerEntry>());

        Assert.Contains("AnalysisEndTimestamp", result.AnalyticsSummary);
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

        var result = _engine.ExecuteAnalytics(command, ledger);

        Assert.Equal(1000m, result.CurrentBalance);
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

        var result = _engine.ExecuteAnalytics(command, ledger);

        Assert.Equal(500m, result.CurrentBalance);
    }

    [Fact]
    public void EmptyLedger_ReturnsZeroMetrics()
    {
        var command = CreateCommand();

        var result = _engine.ExecuteAnalytics(command, Array.Empty<LedgerEntry>());

        Assert.Equal(0m, result.CurrentBalance);
        Assert.Equal(0m, result.AverageBalance);
        Assert.Equal(0m, result.MinimumBalance);
        Assert.Equal(0m, result.MaximumBalance);
        Assert.Equal(0m, result.BalanceGrowthRate);
        Assert.Equal("Stable", result.BalanceTrend);
    }
}
