namespace Whycespace.Simulation;

using global::System.Collections.Concurrent;
using global::System.Diagnostics;

public sealed class SimulationMetrics
{
    private long _totalExecuted;
    private long _totalSucceeded;
    private long _totalFailed;
    private long _totalEngineInvocations;
    private long _totalEventsPublished;
    private long _totalProjectionUpdates;
    private long _totalRetriesAttempted;
    private long _totalDeadLettered;

    private readonly ConcurrentBag<double> _workflowLatenciesMs = new();
    private readonly ConcurrentBag<double> _engineLatenciesMs = new();
    private readonly ConcurrentBag<double> _projectionLatenciesMs = new();
    private readonly ConcurrentDictionary<string, long> _workflowTypeCounts = new();
    private readonly ConcurrentDictionary<string, long> _engineInvocationCounts = new();

    private readonly Stopwatch _wallClock = new();

    public void Start() => _wallClock.Start();
    public void Stop() => _wallClock.Stop();
    public TimeSpan Elapsed => _wallClock.Elapsed;

    public void RecordWorkflowCompleted(string workflowType, double latencyMs, bool success)
    {
        Interlocked.Increment(ref _totalExecuted);
        _workflowLatenciesMs.Add(latencyMs);
        _workflowTypeCounts.AddOrUpdate(workflowType, 1, (_, c) => c + 1);

        if (success)
            Interlocked.Increment(ref _totalSucceeded);
        else
            Interlocked.Increment(ref _totalFailed);
    }

    public void RecordEngineInvocation(string engineName, double latencyMs)
    {
        Interlocked.Increment(ref _totalEngineInvocations);
        _engineLatenciesMs.Add(latencyMs);
        _engineInvocationCounts.AddOrUpdate(engineName, 1, (_, c) => c + 1);
    }

    public void RecordEventPublished() => Interlocked.Increment(ref _totalEventsPublished);

    public void RecordProjectionUpdate(double latencyMs)
    {
        Interlocked.Increment(ref _totalProjectionUpdates);
        _projectionLatenciesMs.Add(latencyMs);
    }

    public void RecordRetry() => Interlocked.Increment(ref _totalRetriesAttempted);
    public void RecordDeadLetter() => Interlocked.Increment(ref _totalDeadLettered);

    public long TotalExecuted => Interlocked.Read(ref _totalExecuted);
    public long TotalSucceeded => Interlocked.Read(ref _totalSucceeded);
    public long TotalFailed => Interlocked.Read(ref _totalFailed);
    public long TotalEngineInvocations => Interlocked.Read(ref _totalEngineInvocations);
    public long TotalEventsPublished => Interlocked.Read(ref _totalEventsPublished);
    public long TotalProjectionUpdates => Interlocked.Read(ref _totalProjectionUpdates);
    public long TotalRetries => Interlocked.Read(ref _totalRetriesAttempted);
    public long TotalDeadLettered => Interlocked.Read(ref _totalDeadLettered);

    public double SuccessRate => TotalExecuted == 0 ? 0 : (double)TotalSucceeded / TotalExecuted * 100;

    public double AverageWorkflowLatencyMs => ComputeAverage(_workflowLatenciesMs);
    public double P95WorkflowLatencyMs => ComputePercentile(_workflowLatenciesMs, 0.95);
    public double P99WorkflowLatencyMs => ComputePercentile(_workflowLatenciesMs, 0.99);
    public double MaxWorkflowLatencyMs => _workflowLatenciesMs.IsEmpty ? 0 : _workflowLatenciesMs.Max();

    public double AverageEngineLatencyMs => ComputeAverage(_engineLatenciesMs);
    public double AverageProjectionLatencyMs => ComputeAverage(_projectionLatenciesMs);

    public double WorkflowsPerSecond => Elapsed.TotalSeconds == 0 ? 0 : TotalExecuted / Elapsed.TotalSeconds;
    public double EventsPerSecond => Elapsed.TotalSeconds == 0 ? 0 : TotalEventsPublished / Elapsed.TotalSeconds;
    public double EngineInvocationsPerSecond => Elapsed.TotalSeconds == 0 ? 0 : TotalEngineInvocations / Elapsed.TotalSeconds;

    public IReadOnlyDictionary<string, long> WorkflowTypeCounts => _workflowTypeCounts;
    public IReadOnlyDictionary<string, long> EngineInvocationCounts => _engineInvocationCounts;

    private static double ComputeAverage(ConcurrentBag<double> values)
    {
        if (values.IsEmpty) return 0;
        var arr = values.ToArray();
        return arr.Average();
    }

    private static double ComputePercentile(ConcurrentBag<double> values, double percentile)
    {
        if (values.IsEmpty) return 0;
        var sorted = values.ToArray();
        Array.Sort(sorted);
        var index = (int)Math.Ceiling(percentile * sorted.Length) - 1;
        return sorted[Math.Max(0, index)];
    }
}
