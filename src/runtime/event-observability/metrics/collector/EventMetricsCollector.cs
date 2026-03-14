using Whycespace.EventObservability.Metrics.Models;
using Whycespace.EventObservability.Metrics.Snapshot;

namespace Whycespace.EventObservability.Metrics.Collector;

public sealed class EventMetricsCollector
{
    private long _eventsProcessed;
    private long _eventsSucceeded;
    private long _eventsFailed;

    private long _retryAttempts;
    private long _deadLetterEvents;
    private long _engineFailures;
    private long _infrastructureFailures;

    private long _replayAttempts;
    private long _replaySucceeded;
    private long _replayRejected;

    private long _partitionsHealthy;
    private long _partitionsDegraded;
    private long _partitionsCircuitOpen;

    public void RecordEventProcessed()
    {
        Interlocked.Increment(ref _eventsProcessed);
    }

    public void RecordEventSucceeded()
    {
        Interlocked.Increment(ref _eventsSucceeded);
    }

    public void RecordEventFailed()
    {
        Interlocked.Increment(ref _eventsFailed);
    }

    public void RecordRetryAttempt()
    {
        Interlocked.Increment(ref _retryAttempts);
    }

    public void RecordDeadLetterEvent()
    {
        Interlocked.Increment(ref _deadLetterEvents);
    }

    public void RecordEngineFailure()
    {
        Interlocked.Increment(ref _engineFailures);
    }

    public void RecordInfrastructureFailure()
    {
        Interlocked.Increment(ref _infrastructureFailures);
    }

    public void RecordReplayAttempt()
    {
        Interlocked.Increment(ref _replayAttempts);
    }

    public void RecordReplaySucceeded()
    {
        Interlocked.Increment(ref _replaySucceeded);
    }

    public void RecordReplayRejected()
    {
        Interlocked.Increment(ref _replayRejected);
    }

    public void RecordPartitionHealthy()
    {
        Interlocked.Increment(ref _partitionsHealthy);
    }

    public void RecordPartitionDegraded()
    {
        Interlocked.Increment(ref _partitionsDegraded);
    }

    public void RecordPartitionCircuitOpen()
    {
        Interlocked.Increment(ref _partitionsCircuitOpen);
    }

    public void SetPartitionMetrics(long healthy, long degraded, long circuitOpen)
    {
        Interlocked.Exchange(ref _partitionsHealthy, healthy);
        Interlocked.Exchange(ref _partitionsDegraded, degraded);
        Interlocked.Exchange(ref _partitionsCircuitOpen, circuitOpen);
    }

    public Models.EventMetrics GetEventMetrics()
    {
        return new Models.EventMetrics(
            Interlocked.Read(ref _eventsProcessed),
            Interlocked.Read(ref _eventsSucceeded),
            Interlocked.Read(ref _eventsFailed));
    }

    public FailureMetrics GetFailureMetrics()
    {
        return new FailureMetrics(
            Interlocked.Read(ref _retryAttempts),
            Interlocked.Read(ref _deadLetterEvents),
            Interlocked.Read(ref _engineFailures),
            Interlocked.Read(ref _infrastructureFailures));
    }

    public ReplayMetrics GetReplayMetrics()
    {
        return new ReplayMetrics(
            Interlocked.Read(ref _replayAttempts),
            Interlocked.Read(ref _replaySucceeded),
            Interlocked.Read(ref _replayRejected));
    }

    public PartitionMetrics GetPartitionMetrics()
    {
        return new PartitionMetrics(
            Interlocked.Read(ref _partitionsHealthy),
            Interlocked.Read(ref _partitionsDegraded),
            Interlocked.Read(ref _partitionsCircuitOpen));
    }

    public RuntimeMetricsSnapshot GetSnapshot()
    {
        return new RuntimeMetricsSnapshot(
            GetEventMetrics(),
            GetFailureMetrics(),
            GetReplayMetrics(),
            GetPartitionMetrics());
    }
}
