namespace Whycespace.Engines.T3I.Forecasting.Policy.Models;

public sealed record PolicyImpactForecastResult(
    IReadOnlyList<PolicyImpactRecord> ForecastRecords,
    IReadOnlyList<string> AffectedPolicies,
    ImpactSeverity RiskLevel,
    DateTime GeneratedAt
);
