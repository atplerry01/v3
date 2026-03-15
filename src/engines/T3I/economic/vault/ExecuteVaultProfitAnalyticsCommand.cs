namespace Whycespace.Engines.T3I.Economic.Vault;

public sealed record ExecuteVaultProfitAnalyticsCommand(
    Guid AnalyticsId,
    Guid VaultId,
    DateTime AnalysisStartTimestamp,
    DateTime AnalysisEndTimestamp,
    string AnalysisScope,
    Guid RequestedBy,
    string? ReferenceId = null,
    string? ReferenceType = null);

public enum ProfitAnalysisScope
{
    ProfitGeneration,
    ProfitDistribution,
    ParticipantProfitExposure,
    FullProfitAnalysis
}
