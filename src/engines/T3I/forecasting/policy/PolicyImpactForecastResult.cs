namespace Whycespace.Engines.T3I.Forecasting.Policy;

public sealed record PolicyImpactForecastResult(
    IReadOnlyList<PolicyImpactRecord> ForecastRecords,
    IReadOnlyList<string> AffectedPolicies,
    ImpactSeverity RiskLevel,
    DateTime GeneratedAt
);
