namespace Whycespace.Engines.T3I.HEOS;

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
