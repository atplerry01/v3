namespace Whycespace.Engines.T3I.WhycePolicy;

public sealed record PolicyMonitoringReport(
    string PolicyId,
    TimeRange ObservationWindow,
    PolicyDecisionStatistics DecisionStatistics,
    IReadOnlyList<PolicyAnomalyRecord> AnomalyRecords,
    DateTime GeneratedAt
);

public sealed record PolicyDecisionStatistics(
    int TotalEvaluations,
    int TotalDenials,
    int TotalAllowed,
    int TotalEscalations,
    double DenialRate,
    double EscalationRate
);
