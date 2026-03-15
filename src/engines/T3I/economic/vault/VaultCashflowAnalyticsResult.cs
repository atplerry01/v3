namespace Whycespace.Engines.T3I.Economic.Vault;

public sealed record VaultCashflowAnalyticsResult(
    Guid AnalyticsId,
    Guid VaultId,
    decimal TotalInflows,
    decimal TotalOutflows,
    decimal NetCashflow,
    int ContributionCount,
    int WithdrawalCount,
    int TransferCount,
    string CashflowTrend,
    string AnalyticsSummary,
    DateTime CompletedAt,
    string? AnalyticsHash = null);
