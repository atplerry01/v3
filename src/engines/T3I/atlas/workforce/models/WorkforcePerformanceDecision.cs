namespace Whycespace.Engines.T3I.Atlas.Workforce.Models;

public sealed record WorkforcePerformanceDecision(
    decimal PerformanceScore,
    PerformanceTier PerformanceTier,
    string EvaluationSummary
);

public enum PerformanceTier
{
    Low,
    Standard,
    High,
    Exceptional
}
