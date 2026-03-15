namespace Whycespace.Engines.T3I.Economic.Vault;

public sealed record ExecuteVaultCashflowAnalyticsCommand(
    Guid AnalyticsId,
    Guid VaultId,
    DateTime AnalysisStartTimestamp,
    DateTime AnalysisEndTimestamp,
    string AnalysisScope,
    Guid RequestedBy,
    string? ReferenceId = null,
    string? ReferenceType = null);

public enum CashflowAnalysisScope
{
    ContributionFlow,
    WithdrawalFlow,
    TransferFlow,
    FullCashflow
}
