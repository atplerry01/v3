namespace Whycespace.Engines.T3I.Economic.Vault;

public sealed record VaultBalanceAnalyticsResult(
    Guid AnalyticsId,
    Guid VaultId,
    decimal CurrentBalance,
    decimal AverageBalance,
    decimal MinimumBalance,
    decimal MaximumBalance,
    decimal BalanceGrowthRate,
    string BalanceTrend,
    string AnalyticsSummary,
    DateTime CompletedAt,
    string AnalyticsHash = "");
