namespace Whycespace.Tests.Engines.Vault;

using Whycespace.Engines.T3I.Atlas.Economic;
using Xunit;

public sealed class VaultProfitAnalyticsEngineTests
{
    private static readonly Guid VaultId = Guid.NewGuid();
    private static readonly Guid RequestedBy = Guid.NewGuid();
    private static readonly DateTime Start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc);

    private static ExecuteVaultProfitAnalyticsCommand CreateCommand(
        string scope = "FullProfitAnalysis",
        Guid? analyticsId = null,
        Guid? vaultId = null,
        Guid? requestedBy = null,
        DateTime? start = null,
        DateTime? end = null)
    {
        return new ExecuteVaultProfitAnalyticsCommand(
            AnalyticsId: analyticsId ?? Guid.NewGuid(),
            VaultId: vaultId ?? VaultId,
            AnalysisStartTimestamp: start ?? Start,
            AnalysisEndTimestamp: end ?? End,
            AnalysisScope: scope,
            RequestedBy: requestedBy ?? RequestedBy);
    }

    [Fact]
    public void ProfitTotalsTest_GeneratedAndDistributedCalculatedCorrectly()
    {
        var command = CreateCommand();
        var ledger = new List<LedgerEntry>
        {
            new(Guid.NewGuid(), VaultId, "RevenueAllocation", 500_000m, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "InvestmentReturn", 300_000m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "ExternalYield", 200_000m, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "ParticipantProfitDistribution", 400_000m, new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "OperatorProfitDistribution", 100_000m, new DateTime(2026, 1, 25, 0, 0, 0, DateTimeKind.Utc))
        };

        var result = VaultProfitAnalyticsEngine.ComputeAnalytics(command, ledger);

        Assert.Equal(1_000_000m, result.TotalProfitGenerated);
        Assert.Equal(500_000m, result.TotalProfitDistributed);
    }

    [Fact]
    public void ProfitRetentionTest_RetainedProfitCalculatedCorrectly()
    {
        var command = CreateCommand();
        var ledger = new List<LedgerEntry>
        {
            new(Guid.NewGuid(), VaultId, "RevenueAllocation", 1_200_000m, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "ParticipantProfitDistribution", 800_000m, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc))
        };

        var result = VaultProfitAnalyticsEngine.ComputeAnalytics(command, ledger);

        Assert.Equal(1_200_000m, result.TotalProfitGenerated);
        Assert.Equal(800_000m, result.TotalProfitDistributed);
        Assert.Equal(400_000m, result.RetainedProfit);
    }

    [Fact]
    public void DistributionCountTest_CountIsCorrect()
    {
        var command = CreateCommand();
        var ledger = new List<LedgerEntry>
        {
            new(Guid.NewGuid(), VaultId, "RevenueAllocation", 500_000m, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "ParticipantProfitDistribution", 100_000m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "OperatorProfitDistribution", 50_000m, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "GovernanceDistribution", 25_000m, new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc))
        };

        var result = VaultProfitAnalyticsEngine.ComputeAnalytics(command, ledger);

        Assert.Equal(3, result.ProfitDistributionCount);
        Assert.Equal(Math.Round(175_000m / 3m, 2), result.AverageProfitPerDistribution);
    }

    [Fact]
    public void TrendAnalysisTest_IncreasingWhenHighRetention()
    {
        var trend = VaultProfitAnalyticsEngine.DetermineTrend(1_000_000m, 500_000m);

        Assert.Equal("Increasing", trend);
    }

    [Fact]
    public void TrendAnalysisTest_DecliningWhenMostDistributed()
    {
        var trend = VaultProfitAnalyticsEngine.DetermineTrend(1_000_000m, 960_000m);

        Assert.Equal("Declining", trend);
    }

    [Fact]
    public void TrendAnalysisTest_StableWhenModerateRetention()
    {
        var trend = VaultProfitAnalyticsEngine.DetermineTrend(1_000_000m, 800_000m);

        Assert.Equal("Stable", trend);
    }

    [Fact]
    public void TrendAnalysisTest_NeutralWhenNoActivity()
    {
        var trend = VaultProfitAnalyticsEngine.DetermineTrend(0m, 0m);

        Assert.Equal("Neutral", trend);
    }

    [Fact]
    public void DeterministicAnalyticsTest_SameInputProducesSameOutput()
    {
        var analyticsId = Guid.NewGuid();
        var command = CreateCommand(analyticsId: analyticsId);
        var ledger = new List<LedgerEntry>
        {
            new(Guid.NewGuid(), VaultId, "RevenueAllocation", 1_000_000m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "ParticipantProfitDistribution", 300_000m, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc))
        };

        var result1 = VaultProfitAnalyticsEngine.ComputeAnalytics(command, ledger);
        var result2 = VaultProfitAnalyticsEngine.ComputeAnalytics(command, ledger);

        Assert.Equal(result1.AnalyticsId, result2.AnalyticsId);
        Assert.Equal(result1.TotalProfitGenerated, result2.TotalProfitGenerated);
        Assert.Equal(result1.TotalProfitDistributed, result2.TotalProfitDistributed);
        Assert.Equal(result1.RetainedProfit, result2.RetainedProfit);
        Assert.Equal(result1.AverageProfitPerDistribution, result2.AverageProfitPerDistribution);
        Assert.Equal(result1.ProfitDistributionCount, result2.ProfitDistributionCount);
        Assert.Equal(result1.ProfitTrend, result2.ProfitTrend);
        Assert.Equal(result1.AnalyticsHash, result2.AnalyticsHash);
    }

    [Fact]
    public void ScopeFilter_ProfitGenerationOnly()
    {
        var command = CreateCommand(scope: "ProfitGeneration");
        var ledger = new List<LedgerEntry>
        {
            new(Guid.NewGuid(), VaultId, "RevenueAllocation", 500_000m, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "ParticipantProfitDistribution", 200_000m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc))
        };

        var result = VaultProfitAnalyticsEngine.ComputeAnalytics(command, ledger);

        Assert.Equal(500_000m, result.TotalProfitGenerated);
        Assert.Equal(0m, result.TotalProfitDistributed);
    }

    [Fact]
    public void ScopeFilter_ProfitDistributionOnly()
    {
        var command = CreateCommand(scope: "ProfitDistribution");
        var ledger = new List<LedgerEntry>
        {
            new(Guid.NewGuid(), VaultId, "RevenueAllocation", 500_000m, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "ParticipantProfitDistribution", 200_000m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
            new(Guid.NewGuid(), VaultId, "OperatorProfitDistribution", 100_000m, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc))
        };

        var result = VaultProfitAnalyticsEngine.ComputeAnalytics(command, ledger);

        Assert.Equal(0m, result.TotalProfitGenerated);
        Assert.Equal(300_000m, result.TotalProfitDistributed);
    }

    [Fact]
    public void EmptyLedger_ReturnsZeroMetrics()
    {
        var command = CreateCommand();

        var result = VaultProfitAnalyticsEngine.ComputeAnalytics(command, Array.Empty<LedgerEntry>());

        Assert.Equal(0m, result.TotalProfitGenerated);
        Assert.Equal(0m, result.TotalProfitDistributed);
        Assert.Equal(0m, result.RetainedProfit);
        Assert.Equal(0m, result.AverageProfitPerDistribution);
        Assert.Equal(0, result.ProfitDistributionCount);
    }

    [Fact]
    public void ClassifyEntry_GenerationTypes()
    {
        Assert.Equal(ProfitClassification.Generation, VaultProfitAnalyticsEngine.ClassifyEntry("RevenueAllocation"));
        Assert.Equal(ProfitClassification.Generation, VaultProfitAnalyticsEngine.ClassifyEntry("InvestmentReturn"));
        Assert.Equal(ProfitClassification.Generation, VaultProfitAnalyticsEngine.ClassifyEntry("ExternalYield"));
    }

    [Fact]
    public void ClassifyEntry_DistributionTypes()
    {
        Assert.Equal(ProfitClassification.Distribution, VaultProfitAnalyticsEngine.ClassifyEntry("ParticipantProfitDistribution"));
        Assert.Equal(ProfitClassification.Distribution, VaultProfitAnalyticsEngine.ClassifyEntry("OperatorProfitDistribution"));
        Assert.Equal(ProfitClassification.Distribution, VaultProfitAnalyticsEngine.ClassifyEntry("GovernanceDistribution"));
    }

    [Fact]
    public void ClassifyEntry_UnknownTypes()
    {
        Assert.Equal(ProfitClassification.Unknown, VaultProfitAnalyticsEngine.ClassifyEntry("Contribution"));
        Assert.Equal(ProfitClassification.Unknown, VaultProfitAnalyticsEngine.ClassifyEntry("Withdrawal"));
    }
}
