namespace Whycespace.VaultCashflowAnalytics.Tests;

using Whycespace.Engines.T3I.Economic.Vault;
using Whycespace.Contracts.Engines;

public sealed class VaultCashflowAnalyticsEngineTests
{
    private readonly VaultCashflowAnalyticsEngine _engine = new();

    private static readonly DateTime FixedStart = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FixedEnd = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    // --- CashflowTotalsTest ---

    [Fact]
    public async Task CashflowTotals_InflowsAndOutflows_CalculatedCorrectly()
    {
        var context = CreateContext(new List<Dictionary<string, object>>
        {
            LedgerEntry("Contribution", 100_000m),
            LedgerEntry("Contribution", 50_000m),
            LedgerEntry("TransferIn", 25_000m),
            LedgerEntry("Withdrawal", 30_000m),
            LedgerEntry("TransferOut", 10_000m)
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(175_000m, (decimal)result.Output["totalInflows"]);
        Assert.Equal(40_000m, (decimal)result.Output["totalOutflows"]);
    }

    [Fact]
    public async Task CashflowTotals_NoEntries_ReturnsZeros()
    {
        var context = CreateContext(new List<Dictionary<string, object>>());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(0m, (decimal)result.Output["totalInflows"]);
        Assert.Equal(0m, (decimal)result.Output["totalOutflows"]);
        Assert.Equal(0m, (decimal)result.Output["netCashflow"]);
    }

    // --- NetCashflowTest ---

    [Fact]
    public async Task NetCashflow_InflowsExceedOutflows_ReturnsPositive()
    {
        var context = CreateContext(new List<Dictionary<string, object>>
        {
            LedgerEntry("Contribution", 500_000m),
            LedgerEntry("Withdrawal", 200_000m)
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(300_000m, (decimal)result.Output["netCashflow"]);
    }

    [Fact]
    public async Task NetCashflow_OutflowsExceedInflows_ReturnsNegative()
    {
        var context = CreateContext(new List<Dictionary<string, object>>
        {
            LedgerEntry("Contribution", 100_000m),
            LedgerEntry("Withdrawal", 250_000m)
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(-150_000m, (decimal)result.Output["netCashflow"]);
    }

    // --- TrendClassificationTest ---

    [Fact]
    public async Task TrendClassification_PositiveCashflow()
    {
        var context = CreateContext(new List<Dictionary<string, object>>
        {
            LedgerEntry("Contribution", 500_000m),
            LedgerEntry("Withdrawal", 200_000m)
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Positive", result.Output["cashflowTrend"]);
    }

    [Fact]
    public async Task TrendClassification_NegativeCashflow()
    {
        var context = CreateContext(new List<Dictionary<string, object>>
        {
            LedgerEntry("Contribution", 100_000m),
            LedgerEntry("Withdrawal", 300_000m)
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Negative", result.Output["cashflowTrend"]);
    }

    [Fact]
    public async Task TrendClassification_NeutralCashflow()
    {
        var context = CreateContext(new List<Dictionary<string, object>>
        {
            LedgerEntry("Contribution", 100_000m),
            LedgerEntry("Withdrawal", 100_000m)
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Neutral", result.Output["cashflowTrend"]);
    }

    [Fact]
    public async Task TrendClassification_NoEntries_Neutral()
    {
        var context = CreateContext(new List<Dictionary<string, object>>());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Neutral", result.Output["cashflowTrend"]);
    }

    // --- TransactionClassificationTest ---

    [Fact]
    public void TransactionClassification_InflowTypes_ClassifiedCorrectly()
    {
        Assert.Equal(EntryClassification.Inflow, VaultCashflowAnalyticsEngine.ClassifyEntry("Contribution"));
        Assert.Equal(EntryClassification.Inflow, VaultCashflowAnalyticsEngine.ClassifyEntry("TransferIn"));
        Assert.Equal(EntryClassification.Inflow, VaultCashflowAnalyticsEngine.ClassifyEntry("ProfitDistributionIn"));
    }

    [Fact]
    public void TransactionClassification_OutflowTypes_ClassifiedCorrectly()
    {
        Assert.Equal(EntryClassification.Outflow, VaultCashflowAnalyticsEngine.ClassifyEntry("Withdrawal"));
        Assert.Equal(EntryClassification.Outflow, VaultCashflowAnalyticsEngine.ClassifyEntry("TransferOut"));
        Assert.Equal(EntryClassification.Outflow, VaultCashflowAnalyticsEngine.ClassifyEntry("ProfitDistributionOut"));
    }

    [Fact]
    public void TransactionClassification_UnknownType_ReturnsUnknown()
    {
        Assert.Equal(EntryClassification.Unknown, VaultCashflowAnalyticsEngine.ClassifyEntry("SomeOtherType"));
    }

    [Fact]
    public async Task TransactionCounts_ClassifiedCorrectly()
    {
        var context = CreateContext(new List<Dictionary<string, object>>
        {
            LedgerEntry("Contribution", 10_000m),
            LedgerEntry("Contribution", 20_000m),
            LedgerEntry("Withdrawal", 5_000m),
            LedgerEntry("TransferIn", 3_000m),
            LedgerEntry("TransferOut", 1_000m)
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(2, (int)result.Output["contributionCount"]);
        Assert.Equal(1, (int)result.Output["withdrawalCount"]);
        Assert.Equal(2, (int)result.Output["transferCount"]);
    }

    // --- DeterministicAnalyticsTest ---

    [Fact]
    public void DeterministicAnalytics_SameInputs_SameResult()
    {
        var analyticsId = Guid.NewGuid();
        var vaultId = Guid.NewGuid();
        var requestedBy = Guid.NewGuid();

        var command = new ExecuteVaultCashflowAnalyticsCommand(
            analyticsId, vaultId, FixedStart, FixedEnd, "FullCashflow", requestedBy);

        var entries = new List<LedgerEntry>
        {
            new(Guid.NewGuid(), vaultId, "Contribution", 100_000m, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), vaultId, "Withdrawal", 30_000m, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), vaultId, "TransferIn", 15_000m, new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc))
        };

        var result1 = VaultCashflowAnalyticsEngine.ComputeAnalytics(command, entries);
        var result2 = VaultCashflowAnalyticsEngine.ComputeAnalytics(command, entries);

        Assert.Equal(result1.TotalInflows, result2.TotalInflows);
        Assert.Equal(result1.TotalOutflows, result2.TotalOutflows);
        Assert.Equal(result1.NetCashflow, result2.NetCashflow);
        Assert.Equal(result1.CashflowTrend, result2.CashflowTrend);
        Assert.Equal(result1.AnalyticsHash, result2.AnalyticsHash);
    }

    [Fact]
    public async Task DeterministicAnalytics_SameContextData_SameOutput()
    {
        var data = CreateContextData(new List<Dictionary<string, object>>
        {
            LedgerEntry("Contribution", 50_000m),
            LedgerEntry("Withdrawal", 20_000m)
        });

        var context1 = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "AnalyzeCashflow",
            "partition-1", data);

        var context2 = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "AnalyzeCashflow",
            "partition-1", data);

        var result1 = await _engine.ExecuteAsync(context1);
        var result2 = await _engine.ExecuteAsync(context2);

        Assert.Equal(result1.Output["totalInflows"], result2.Output["totalInflows"]);
        Assert.Equal(result1.Output["totalOutflows"], result2.Output["totalOutflows"]);
        Assert.Equal(result1.Output["netCashflow"], result2.Output["netCashflow"]);
        Assert.Equal(result1.Output["cashflowTrend"], result2.Output["cashflowTrend"]);
        Assert.Equal(result1.Output["analyticsHash"], result2.Output["analyticsHash"]);
    }

    // --- Scope filtering ---

    [Fact]
    public async Task Scope_ContributionFlow_OnlyIncludesContributions()
    {
        var context = CreateContext(
            new List<Dictionary<string, object>>
            {
                LedgerEntry("Contribution", 100_000m),
                LedgerEntry("Withdrawal", 50_000m),
                LedgerEntry("TransferIn", 25_000m)
            },
            scope: "ContributionFlow");

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(100_000m, (decimal)result.Output["totalInflows"]);
        Assert.Equal(0m, (decimal)result.Output["totalOutflows"]);
    }

    [Fact]
    public async Task Scope_WithdrawalFlow_OnlyIncludesWithdrawals()
    {
        var context = CreateContext(
            new List<Dictionary<string, object>>
            {
                LedgerEntry("Contribution", 100_000m),
                LedgerEntry("Withdrawal", 50_000m),
                LedgerEntry("TransferOut", 25_000m)
            },
            scope: "WithdrawalFlow");

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(0m, (decimal)result.Output["totalInflows"]);
        Assert.Equal(50_000m, (decimal)result.Output["totalOutflows"]);
    }

    [Fact]
    public async Task Scope_TransferFlow_OnlyIncludesTransfers()
    {
        var context = CreateContext(
            new List<Dictionary<string, object>>
            {
                LedgerEntry("Contribution", 100_000m),
                LedgerEntry("TransferIn", 25_000m),
                LedgerEntry("TransferOut", 10_000m)
            },
            scope: "TransferFlow");

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(25_000m, (decimal)result.Output["totalInflows"]);
        Assert.Equal(10_000m, (decimal)result.Output["totalOutflows"]);
    }

    // --- Validation tests ---

    [Fact]
    public async Task MissingVaultId_Fails()
    {
        var data = new Dictionary<string, object>
        {
            ["analyticsId"] = Guid.NewGuid().ToString(),
            ["requestedBy"] = Guid.NewGuid().ToString(),
            ["analysisScope"] = "FullCashflow",
            ["analysisStartTimestamp"] = FixedStart.ToString("O"),
            ["analysisEndTimestamp"] = FixedEnd.ToString("O")
        };

        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "AnalyzeCashflow",
            "partition-1", data);

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingAnalyticsId_Fails()
    {
        var data = new Dictionary<string, object>
        {
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["requestedBy"] = Guid.NewGuid().ToString(),
            ["analysisScope"] = "FullCashflow",
            ["analysisStartTimestamp"] = FixedStart.ToString("O"),
            ["analysisEndTimestamp"] = FixedEnd.ToString("O")
        };

        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "AnalyzeCashflow",
            "partition-1", data);

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task InvalidScope_Fails()
    {
        var context = CreateContext(
            new List<Dictionary<string, object>>(),
            scope: "InvalidScope");

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task EndBeforeStart_Fails()
    {
        var data = CreateContextData(
            new List<Dictionary<string, object>>(),
            startTimestamp: FixedEnd,
            endTimestamp: FixedStart);

        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "AnalyzeCashflow",
            "partition-1", data);

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    // --- Event emission ---

    [Fact]
    public async Task CompletedAnalytics_EmitsCorrectEvent()
    {
        var vaultId = Guid.NewGuid();
        var context = CreateContext(
            new List<Dictionary<string, object>>
            {
                LedgerEntry("Contribution", 100_000m)
            },
            vaultId: vaultId);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("VaultCashflowAnalyticsCompleted", result.Events[0].EventType);
        Assert.Equal(vaultId, result.Events[0].AggregateId);
    }

    // --- Concurrency ---

    [Fact]
    public async Task ConcurrentExecutions_AllSucceed()
    {
        var tasks = Enumerable.Range(0, 50).Select(_ =>
        {
            var context = CreateContext(new List<Dictionary<string, object>>
            {
                LedgerEntry("Contribution", 10_000m),
                LedgerEntry("Withdrawal", 3_000m)
            });
            return _engine.ExecuteAsync(context);
        });

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r =>
        {
            Assert.True(r.Success);
            Assert.Single(r.Events);
            Assert.Equal(10_000m, (decimal)r.Output["totalInflows"]);
            Assert.Equal(3_000m, (decimal)r.Output["totalOutflows"]);
        });
    }

    // --- Helpers ---

    private static Dictionary<string, object> LedgerEntry(string entryType, decimal amount)
    {
        return new Dictionary<string, object>
        {
            ["entryType"] = entryType,
            ["amount"] = amount,
            ["timestamp"] = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc).ToString("O")
        };
    }

    private static Dictionary<string, object> CreateContextData(
        List<Dictionary<string, object>> ledgerEntries,
        Guid? vaultId = null,
        string scope = "FullCashflow",
        DateTime? startTimestamp = null,
        DateTime? endTimestamp = null)
    {
        return new Dictionary<string, object>
        {
            ["analyticsId"] = Guid.NewGuid().ToString(),
            ["vaultId"] = (vaultId ?? Guid.NewGuid()).ToString(),
            ["requestedBy"] = Guid.NewGuid().ToString(),
            ["analysisScope"] = scope,
            ["analysisStartTimestamp"] = (startTimestamp ?? FixedStart).ToString("O"),
            ["analysisEndTimestamp"] = (endTimestamp ?? FixedEnd).ToString("O"),
            ["ledgerEntries"] = ledgerEntries.Cast<object>().ToList()
        };
    }

    private static EngineContext CreateContext(
        List<Dictionary<string, object>> ledgerEntries,
        Guid? vaultId = null,
        string scope = "FullCashflow")
    {
        var data = CreateContextData(ledgerEntries, vaultId, scope);
        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "AnalyzeCashflow",
            "partition-1", data);
    }
}
