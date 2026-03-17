namespace Whycespace.IntelligenceRuntime.Tracing;

using Whycespace.IntelligenceRuntime.Models;

public sealed class IntelligenceObservability
{
    private readonly IntelligenceTraceCollector _collector;

    public IntelligenceObservability(IntelligenceTraceCollector collector)
    {
        _collector = collector;
    }

    public IntelligenceHealthReport GetHealthReport()
    {
        var metrics = _collector.GetMetrics();
        var recent = _collector.GetRecent(20);

        var recentFailures = recent.Where(t => t.Success == false).ToList();

        var status = recentFailures.Count switch
        {
            0 => IntelligenceHealthStatus.Healthy,
            <= 3 => IntelligenceHealthStatus.Degraded,
            _ => IntelligenceHealthStatus.Unhealthy
        };

        return new IntelligenceHealthReport(
            status,
            metrics,
            recentFailures.Select(f => f.Error ?? "Unknown").ToList(),
            DateTimeOffset.UtcNow);
    }
}

public enum IntelligenceHealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

public sealed record IntelligenceHealthReport(
    IntelligenceHealthStatus Status,
    IntelligenceMetricsSnapshot Metrics,
    IReadOnlyList<string> RecentErrors,
    DateTimeOffset GeneratedAt
);
