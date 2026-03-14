using Whycespace.EventObservability.Metrics.Collector;
using Whycespace.EventObservability.Metrics.Engine;

namespace Whycespace.EventObservabilityTests;

public sealed class EventObservabilityTests
{
    [Fact]
    public void RecordEventProcessed_IncrementsCounter()
    {
        var collector = new EventMetricsCollector();

        collector.RecordEventProcessed();
        collector.RecordEventProcessed();
        collector.RecordEventProcessed();

        var metrics = collector.GetEventMetrics();
        Assert.Equal(3, metrics.EventsProcessed);
    }

    [Fact]
    public void RecordEventSucceeded_IncrementsCounter()
    {
        var collector = new EventMetricsCollector();

        collector.RecordEventSucceeded();
        collector.RecordEventSucceeded();

        var metrics = collector.GetEventMetrics();
        Assert.Equal(2, metrics.EventsSucceeded);
    }

    [Fact]
    public void RecordEventFailed_IncrementsCounter()
    {
        var collector = new EventMetricsCollector();

        collector.RecordEventFailed();

        var metrics = collector.GetEventMetrics();
        Assert.Equal(1, metrics.EventsFailed);
    }

    [Fact]
    public void RecordRetryAttempt_IncrementsRetryCounter()
    {
        var collector = new EventMetricsCollector();

        collector.RecordRetryAttempt();
        collector.RecordRetryAttempt();
        collector.RecordRetryAttempt();

        var metrics = collector.GetFailureMetrics();
        Assert.Equal(3, metrics.RetryAttempts);
    }

    [Fact]
    public void RecordDeadLetterEvent_IncrementsDeadLetterCounter()
    {
        var collector = new EventMetricsCollector();

        collector.RecordDeadLetterEvent();
        collector.RecordDeadLetterEvent();

        var metrics = collector.GetFailureMetrics();
        Assert.Equal(2, metrics.DeadLetterEvents);
    }

    [Fact]
    public void RecordEngineFailure_IncrementsEngineFailureCounter()
    {
        var collector = new EventMetricsCollector();

        collector.RecordEngineFailure();

        var metrics = collector.GetFailureMetrics();
        Assert.Equal(1, metrics.EngineFailures);
    }

    [Fact]
    public void RecordInfrastructureFailure_IncrementsInfrastructureFailureCounter()
    {
        var collector = new EventMetricsCollector();

        collector.RecordInfrastructureFailure();
        collector.RecordInfrastructureFailure();

        var metrics = collector.GetFailureMetrics();
        Assert.Equal(2, metrics.InfrastructureFailures);
    }

    [Fact]
    public void RecordReplayAttempt_IncrementsReplayMetrics()
    {
        var collector = new EventMetricsCollector();

        collector.RecordReplayAttempt();
        collector.RecordReplaySucceeded();
        collector.RecordReplayRejected();
        collector.RecordReplayRejected();

        var metrics = collector.GetReplayMetrics();
        Assert.Equal(1, metrics.ReplayAttempts);
        Assert.Equal(1, metrics.ReplaySucceeded);
        Assert.Equal(2, metrics.ReplayRejected);
    }

    [Fact]
    public void GetSnapshot_ReturnsCorrectValues()
    {
        var collector = new EventMetricsCollector();

        collector.RecordEventProcessed();
        collector.RecordEventProcessed();
        collector.RecordEventSucceeded();
        collector.RecordEventFailed();
        collector.RecordRetryAttempt();
        collector.RecordDeadLetterEvent();
        collector.RecordEngineFailure();
        collector.RecordInfrastructureFailure();
        collector.RecordReplayAttempt();
        collector.RecordReplaySucceeded();
        collector.RecordReplayRejected();
        collector.RecordPartitionHealthy();
        collector.RecordPartitionDegraded();
        collector.RecordPartitionCircuitOpen();

        var snapshot = collector.GetSnapshot();

        Assert.Equal(2, snapshot.EventMetrics.EventsProcessed);
        Assert.Equal(1, snapshot.EventMetrics.EventsSucceeded);
        Assert.Equal(1, snapshot.EventMetrics.EventsFailed);
        Assert.Equal(1, snapshot.FailureMetrics.RetryAttempts);
        Assert.Equal(1, snapshot.FailureMetrics.DeadLetterEvents);
        Assert.Equal(1, snapshot.FailureMetrics.EngineFailures);
        Assert.Equal(1, snapshot.FailureMetrics.InfrastructureFailures);
        Assert.Equal(1, snapshot.ReplayMetrics.ReplayAttempts);
        Assert.Equal(1, snapshot.ReplayMetrics.ReplaySucceeded);
        Assert.Equal(1, snapshot.ReplayMetrics.ReplayRejected);
        Assert.Equal(1, snapshot.PartitionMetrics.PartitionsHealthy);
        Assert.Equal(1, snapshot.PartitionMetrics.PartitionsDegraded);
        Assert.Equal(1, snapshot.PartitionMetrics.PartitionsCircuitOpen);
    }

    [Fact]
    public void Engine_GetSnapshot_ReturnsCollectorSnapshot()
    {
        var collector = new EventMetricsCollector();
        var engine = new EventObservabilityEngine(collector);

        collector.RecordEventProcessed();
        collector.RecordRetryAttempt();
        collector.RecordReplayAttempt();

        var snapshot = engine.GetSnapshot();

        Assert.Equal(1, snapshot.EventMetrics.EventsProcessed);
        Assert.Equal(1, snapshot.FailureMetrics.RetryAttempts);
        Assert.Equal(1, snapshot.ReplayMetrics.ReplayAttempts);
    }

    [Fact]
    public void ConcurrentAccess_MaintainsAccurateCounters()
    {
        var collector = new EventMetricsCollector();
        var iterations = 10_000;

        Parallel.For(0, iterations, _ =>
        {
            collector.RecordEventProcessed();
            collector.RecordEventFailed();
            collector.RecordRetryAttempt();
            collector.RecordDeadLetterEvent();
            collector.RecordReplayAttempt();
        });

        var snapshot = collector.GetSnapshot();

        Assert.Equal(iterations, snapshot.EventMetrics.EventsProcessed);
        Assert.Equal(iterations, snapshot.EventMetrics.EventsFailed);
        Assert.Equal(iterations, snapshot.FailureMetrics.RetryAttempts);
        Assert.Equal(iterations, snapshot.FailureMetrics.DeadLetterEvents);
        Assert.Equal(iterations, snapshot.ReplayMetrics.ReplayAttempts);
    }

    [Fact]
    public void InitialState_AllCountersZero()
    {
        var collector = new EventMetricsCollector();
        var snapshot = collector.GetSnapshot();

        Assert.Equal(0, snapshot.EventMetrics.EventsProcessed);
        Assert.Equal(0, snapshot.EventMetrics.EventsSucceeded);
        Assert.Equal(0, snapshot.EventMetrics.EventsFailed);
        Assert.Equal(0, snapshot.FailureMetrics.RetryAttempts);
        Assert.Equal(0, snapshot.FailureMetrics.DeadLetterEvents);
        Assert.Equal(0, snapshot.FailureMetrics.EngineFailures);
        Assert.Equal(0, snapshot.FailureMetrics.InfrastructureFailures);
        Assert.Equal(0, snapshot.ReplayMetrics.ReplayAttempts);
        Assert.Equal(0, snapshot.ReplayMetrics.ReplaySucceeded);
        Assert.Equal(0, snapshot.ReplayMetrics.ReplayRejected);
        Assert.Equal(0, snapshot.PartitionMetrics.PartitionsHealthy);
        Assert.Equal(0, snapshot.PartitionMetrics.PartitionsDegraded);
        Assert.Equal(0, snapshot.PartitionMetrics.PartitionsCircuitOpen);
    }

    [Fact]
    public void RecordPartitionHealthy_IncrementsCounter()
    {
        var collector = new EventMetricsCollector();

        collector.RecordPartitionHealthy();
        collector.RecordPartitionHealthy();
        collector.RecordPartitionHealthy();

        var metrics = collector.GetPartitionMetrics();
        Assert.Equal(3, metrics.PartitionsHealthy);
    }

    [Fact]
    public void RecordPartitionDegraded_IncrementsCounter()
    {
        var collector = new EventMetricsCollector();

        collector.RecordPartitionDegraded();
        collector.RecordPartitionDegraded();

        var metrics = collector.GetPartitionMetrics();
        Assert.Equal(2, metrics.PartitionsDegraded);
    }

    [Fact]
    public void RecordPartitionCircuitOpen_IncrementsCounter()
    {
        var collector = new EventMetricsCollector();

        collector.RecordPartitionCircuitOpen();

        var metrics = collector.GetPartitionMetrics();
        Assert.Equal(1, metrics.PartitionsCircuitOpen);
    }

    [Fact]
    public void SetPartitionMetrics_OverwritesCounters()
    {
        var collector = new EventMetricsCollector();

        collector.RecordPartitionHealthy();
        collector.RecordPartitionHealthy();

        collector.SetPartitionMetrics(healthy: 5, degraded: 2, circuitOpen: 1);

        var metrics = collector.GetPartitionMetrics();
        Assert.Equal(5, metrics.PartitionsHealthy);
        Assert.Equal(2, metrics.PartitionsDegraded);
        Assert.Equal(1, metrics.PartitionsCircuitOpen);
    }

    [Fact]
    public void GetSnapshot_IncludesPartitionMetrics()
    {
        var collector = new EventMetricsCollector();

        collector.SetPartitionMetrics(healthy: 8, degraded: 1, circuitOpen: 0);

        var snapshot = collector.GetSnapshot();

        Assert.Equal(8, snapshot.PartitionMetrics.PartitionsHealthy);
        Assert.Equal(1, snapshot.PartitionMetrics.PartitionsDegraded);
        Assert.Equal(0, snapshot.PartitionMetrics.PartitionsCircuitOpen);
    }

    [Fact]
    public void ConcurrentPartitionAccess_MaintainsAccurateCounters()
    {
        var collector = new EventMetricsCollector();
        var iterations = 10_000;

        Parallel.For(0, iterations, _ =>
        {
            collector.RecordPartitionHealthy();
            collector.RecordPartitionDegraded();
            collector.RecordPartitionCircuitOpen();
        });

        var metrics = collector.GetPartitionMetrics();

        Assert.Equal(iterations, metrics.PartitionsHealthy);
        Assert.Equal(iterations, metrics.PartitionsDegraded);
        Assert.Equal(iterations, metrics.PartitionsCircuitOpen);
    }
}
