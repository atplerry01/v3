using System.Collections.Concurrent;
using System.Diagnostics;

namespace Whycespace.Projections.Metrics;

public sealed class ProjectionMetrics
{
    private long _eventsProcessedTotal;
    private long _failuresTotal;
    private long _replayProgress;
    private readonly ConcurrentDictionary<string, long> _eventTypeCounts = new();
    private readonly ConcurrentDictionary<string, long> _latencyTotalMs = new();
    private readonly ConcurrentDictionary<string, long> _latencySamples = new();

    public long EventsProcessedTotal => Interlocked.Read(ref _eventsProcessedTotal);

    public long FailuresTotal => Interlocked.Read(ref _failuresTotal);

    public long ReplayProgress => Interlocked.Read(ref _replayProgress);

    public void RecordEventProcessed(string eventType)
    {
        Interlocked.Increment(ref _eventsProcessedTotal);
        _eventTypeCounts.AddOrUpdate(eventType, 1, (_, v) => v + 1);
    }

    public void RecordFailure(string eventType)
    {
        Interlocked.Increment(ref _failuresTotal);
    }

    public void RecordLatency(string eventType, long elapsedMs)
    {
        _latencyTotalMs.AddOrUpdate(eventType, elapsedMs, (_, v) => v + elapsedMs);
        _latencySamples.AddOrUpdate(eventType, 1, (_, v) => v + 1);
    }

    public void RecordReplayProgress(int processed)
    {
        Interlocked.Exchange(ref _replayProgress, processed);
    }

    public double GetAverageLatencyMs(string eventType)
    {
        if (!_latencyTotalMs.TryGetValue(eventType, out var total))
            return 0;

        if (!_latencySamples.TryGetValue(eventType, out var samples) || samples == 0)
            return 0;

        return (double)total / samples;
    }

    public IReadOnlyDictionary<string, long> GetEventTypeCounts()
    {
        return _eventTypeCounts;
    }

    public ProjectionMetricsSnapshot GetSnapshot()
    {
        return new ProjectionMetricsSnapshot(
            EventsProcessedTotal,
            FailuresTotal,
            ReplayProgress,
            new Dictionary<string, long>(_eventTypeCounts));
    }
}

public sealed record ProjectionMetricsSnapshot(
    long EventsProcessedTotal,
    long FailuresTotal,
    long ReplayProgress,
    IReadOnlyDictionary<string, long> EventTypeCounts
);
