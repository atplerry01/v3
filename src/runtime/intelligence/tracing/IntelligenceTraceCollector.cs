namespace Whycespace.IntelligenceRuntime.Tracing;

using System.Collections.Concurrent;
using Whycespace.IntelligenceRuntime.Models;

public sealed class IntelligenceTraceCollector
{
    private readonly ConcurrentDictionary<Guid, IntelligenceTrace> _traces = new();

    public void Record(IntelligenceTrace trace)
    {
        _traces[trace.TraceId] = trace;
    }

    public IntelligenceTrace? Get(Guid traceId)
        => _traces.GetValueOrDefault(traceId);

    public IReadOnlyList<IntelligenceTrace> GetByRequest(Guid requestId)
        => _traces.Values.Where(t => t.RequestId == requestId).ToList();

    public IReadOnlyList<IntelligenceTrace> GetByCapability(IntelligenceCapability capability)
        => _traces.Values.Where(t => t.Capability == capability).ToList();

    public IReadOnlyList<IntelligenceTrace> GetRecent(int count = 100)
        => _traces.Values
            .OrderByDescending(t => t.StartedAt)
            .Take(count)
            .ToList();

    public IntelligenceMetricsSnapshot GetMetrics()
    {
        var completed = _traces.Values.Where(t => t.CompletedAt.HasValue).ToList();
        var succeeded = completed.Where(t => t.Success == true).ToList();
        var failed = completed.Where(t => t.Success == false).ToList();

        return new IntelligenceMetricsSnapshot(
            TotalExecutions: completed.Count,
            SuccessCount: succeeded.Count,
            FailureCount: failed.Count,
            AverageLatencyMs: completed.Count > 0
                ? completed.Average(t => t.Duration.TotalMilliseconds)
                : 0,
            P95LatencyMs: ComputePercentile(completed, 0.95),
            ByCapability: Enum.GetValues<IntelligenceCapability>()
                .ToDictionary(
                    c => c,
                    c => new CapabilityMetrics(
                        completed.Count(t => t.Capability == c),
                        succeeded.Count(t => t.Capability == c),
                        failed.Count(t => t.Capability == c))));
    }

    private static double ComputePercentile(List<IntelligenceTrace> traces, double percentile)
    {
        if (traces.Count == 0) return 0;

        var sorted = traces
            .Select(t => t.Duration.TotalMilliseconds)
            .OrderBy(d => d)
            .ToList();

        var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
        return sorted[Math.Max(0, index)];
    }
}

public sealed record IntelligenceMetricsSnapshot(
    int TotalExecutions,
    int SuccessCount,
    int FailureCount,
    double AverageLatencyMs,
    double P95LatencyMs,
    IReadOnlyDictionary<IntelligenceCapability, CapabilityMetrics> ByCapability
);

public sealed record CapabilityMetrics(
    int Total,
    int Succeeded,
    int Failed
);
