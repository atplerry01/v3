using System.Collections.Concurrent;

namespace Whycespace.Runtime.Observability.Diagnostics;

public sealed class EngineTelemetryService
{
    private readonly ConcurrentDictionary<string, long> _invocationCounts = new();
    private readonly ConcurrentDictionary<string, long> _successCounts = new();
    private readonly ConcurrentDictionary<string, long> _failureCounts = new();
    private readonly ConcurrentDictionary<string, long> _totalDurationMs = new();

    public void RecordEngineStart(string engineName)
    {
        _invocationCounts.AddOrUpdate(engineName, 1, (_, v) => v + 1);
    }

    public void RecordEngineSuccess(string engineName, long durationMs)
    {
        _successCounts.AddOrUpdate(engineName, 1, (_, v) => v + 1);
        _totalDurationMs.AddOrUpdate(engineName, durationMs, (_, v) => v + durationMs);
    }

    public void RecordEngineFailure(string engineName, long durationMs)
    {
        _failureCounts.AddOrUpdate(engineName, 1, (_, v) => v + 1);
        _totalDurationMs.AddOrUpdate(engineName, durationMs, (_, v) => v + durationMs);
    }

    public long GetInvocationCount(string engineName)
    {
        return _invocationCounts.TryGetValue(engineName, out var v) ? v : 0;
    }

    public long GetSuccessCount(string engineName)
    {
        return _successCounts.TryGetValue(engineName, out var v) ? v : 0;
    }

    public long GetFailureCount(string engineName)
    {
        return _failureCounts.TryGetValue(engineName, out var v) ? v : 0;
    }

    public long GetTotalDurationMs(string engineName)
    {
        return _totalDurationMs.TryGetValue(engineName, out var v) ? v : 0;
    }
}
