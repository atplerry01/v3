namespace Whycespace.Engines.T3I.Atlas.Economic.Models;

public sealed record VaultProfitAnalyticsResult(
    Guid AnalyticsId,
    Guid VaultId,
    decimal TotalProfitGenerated,
    decimal TotalProfitDistributed,
    decimal RetainedProfit,
    decimal AverageProfitPerDistribution,
    int ProfitDistributionCount,
    string ProfitTrend,
    string AnalyticsSummary,
    DateTime CompletedAt,
    string? AnalyticsHash = null);
