namespace Whycespace.Engines.T3I.Economic.Vault;

public enum AnalysisScope
{
    BalanceTrend,
    LiquidityAnalysis,
    AccountDistribution,
    ParticipantExposure
}

public sealed record ExecuteVaultBalanceAnalyticsCommand(
    Guid AnalyticsId,
    Guid VaultId,
    DateTime AnalysisStartTimestamp,
    DateTime AnalysisEndTimestamp,
    AnalysisScope AnalysisScope,
    Guid RequestedBy,
    string ReferenceId = "",
    string ReferenceType = "");
