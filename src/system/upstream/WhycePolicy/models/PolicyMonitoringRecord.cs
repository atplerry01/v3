namespace Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyMonitoringRecord(
    string PolicyId,
    string Domain,
    int Evaluations,
    int AllowedCount,
    int DeniedCount,
    DateTime LastEvaluatedAt
);
