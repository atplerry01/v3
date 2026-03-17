namespace Whycespace.Engines.T2E.Workforce.Models;

public sealed record WorkforceIncentiveCommand(
    Guid WorkforceId,
    decimal PerformanceScore,
    string PerformanceTier,
    DateTimeOffset EvaluationPeriodStart,
    DateTimeOffset EvaluationPeriodEnd,
    decimal BaseIncentiveAmount,
    string Currency,
    string IncentiveType
);
