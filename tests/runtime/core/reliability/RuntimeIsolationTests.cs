using Whycespace.Reliability.Isolation.Models;
using Whycespace.Reliability.Isolation.Monitor;
using Whycespace.Reliability.Isolation.Registry;
using Whycespace.Reliability.Isolation.Engine;

namespace Whycespace.Reliability.Tests;

public sealed class RuntimeIsolationTests
{
    private readonly PartitionHealthRegistry _registry = new();
    private readonly WorkerHealthMonitor _workerMonitor = new();

    // --- Worker Health Monitor Tests ---

    [Fact]
    public void WorkerBecomesUnhealthyAfterFiveConsecutiveFailures()
    {
        for (var i = 0; i < 5; i++)
        {
            _workerMonitor.RecordWorkerFailure("worker-1");
        }

        Assert.Equal(WorkerHealthStatus.Unhealthy, _workerMonitor.GetWorkerHealth("worker-1"));
    }

    [Fact]
    public void WorkerBecomesDegradedAfterTwoConsecutiveFailures()
    {
        _workerMonitor.RecordWorkerFailure("worker-2");
        _workerMonitor.RecordWorkerFailure("worker-2");

        Assert.Equal(WorkerHealthStatus.Degraded, _workerMonitor.GetWorkerHealth("worker-2"));
    }

    [Fact]
    public void WorkerResetsToHealthyOnSuccess()
    {
        for (var i = 0; i < 5; i++)
        {
            _workerMonitor.RecordWorkerFailure("worker-3");
        }

        Assert.Equal(WorkerHealthStatus.Unhealthy, _workerMonitor.GetWorkerHealth("worker-3"));

        _workerMonitor.RecordWorkerSuccess("worker-3");

        Assert.Equal(WorkerHealthStatus.Healthy, _workerMonitor.GetWorkerHealth("worker-3"));
        Assert.Equal(0, _workerMonitor.GetConsecutiveFailures("worker-3"));
    }

    [Fact]
    public void UnknownWorkerIsHealthyByDefault()
    {
        Assert.Equal(WorkerHealthStatus.Healthy, _workerMonitor.GetWorkerHealth("unknown-worker"));
    }

    // --- Partition Circuit Breaker Tests ---

    [Fact]
    public void CircuitBreakerOpensAfterFailureThreshold()
    {
        var engine = new PartitionCircuitBreakerEngine(_registry);

        for (var i = 0; i < 10; i++)
        {
            engine.RecordPartitionFailure(3);
        }

        Assert.Equal(CircuitBreakerState.Open, _registry.GetCircuitState(3));
        Assert.Equal(PartitionHealthStatus.CircuitOpen, _registry.GetPartitionHealth(3));
    }

    [Fact]
    public void CircuitBreakerBlocksProcessingWhenOpen()
    {
        var engine = new PartitionCircuitBreakerEngine(_registry);

        for (var i = 0; i < 10; i++)
        {
            engine.RecordPartitionFailure(4);
        }

        Assert.False(engine.IsPartitionProcessingAllowed(4));
    }

    [Fact]
    public void CircuitBreakerAllowsProcessingWhenClosed()
    {
        var engine = new PartitionCircuitBreakerEngine(_registry);

        Assert.True(engine.IsPartitionProcessingAllowed(5));
    }

    [Fact]
    public void PartitionBecomesDegradedBeforeCircuitOpens()
    {
        var engine = new PartitionCircuitBreakerEngine(_registry);

        for (var i = 0; i < 5; i++)
        {
            engine.RecordPartitionFailure(6);
        }

        Assert.Equal(PartitionHealthStatus.Degraded, _registry.GetPartitionHealth(6));
        Assert.Equal(CircuitBreakerState.Closed, _registry.GetCircuitState(6));
    }

    // --- Half-Open and Recovery Tests ---

    [Fact]
    public void HalfOpenStateAllowsLimitedProcessing()
    {
        var engine = new PartitionCircuitBreakerEngine(_registry);

        for (var i = 0; i < 10; i++)
        {
            engine.RecordPartitionFailure(7);
        }

        Assert.Equal(CircuitBreakerState.Open, _registry.GetCircuitState(7));

        var recovery = new RuntimeRecoveryEngine(_registry);
        recovery.EvaluateRecovery(7);

        Assert.Equal(CircuitBreakerState.HalfOpen, _registry.GetCircuitState(7));
        Assert.True(engine.IsPartitionProcessingAllowed(7));
    }

    [Fact]
    public void PartitionRecoversAfterSuccessfulEventInHalfOpen()
    {
        var engine = new PartitionCircuitBreakerEngine(_registry);

        for (var i = 0; i < 10; i++)
        {
            engine.RecordPartitionFailure(8);
        }

        var recovery = new RuntimeRecoveryEngine(_registry);
        recovery.EvaluateRecovery(8);

        Assert.Equal(CircuitBreakerState.HalfOpen, _registry.GetCircuitState(8));

        engine.RecordPartitionSuccess(8);

        Assert.Equal(CircuitBreakerState.Closed, _registry.GetCircuitState(8));
        Assert.Equal(PartitionHealthStatus.Healthy, _registry.GetPartitionHealth(8));
    }

    [Fact]
    public void RecoveryEngineIdentifiesOpenCircuitsAsCandidates()
    {
        _registry.SetCircuitState(9, CircuitBreakerState.Open);

        var recovery = new RuntimeRecoveryEngine(_registry);

        Assert.True(recovery.IsRecoveryCandidate(9));
        Assert.False(recovery.IsRecoveryCandidate(10));
    }

    [Fact]
    public void RecoveryEngineEvaluatesAllPartitions()
    {
        _registry.SetCircuitState(11, CircuitBreakerState.Open);
        _registry.SetCircuitState(12, CircuitBreakerState.Open);
        _registry.SetCircuitState(13, CircuitBreakerState.Closed);

        var recovery = new RuntimeRecoveryEngine(_registry);
        recovery.EvaluateAllPartitions();

        Assert.Equal(CircuitBreakerState.HalfOpen, _registry.GetCircuitState(11));
        Assert.Equal(CircuitBreakerState.HalfOpen, _registry.GetCircuitState(12));
        Assert.Equal(CircuitBreakerState.Closed, _registry.GetCircuitState(13));
    }

    // --- Registry Tests ---

    [Fact]
    public void RegistryTracksPartitionHealthCorrectly()
    {
        _registry.SetPartitionHealth(1, PartitionHealthStatus.Healthy);
        _registry.SetPartitionHealth(2, PartitionHealthStatus.Degraded);
        _registry.SetPartitionHealth(3, PartitionHealthStatus.CircuitOpen);

        Assert.Equal(PartitionHealthStatus.Healthy, _registry.GetPartitionHealth(1));
        Assert.Equal(PartitionHealthStatus.Degraded, _registry.GetPartitionHealth(2));
        Assert.Equal(PartitionHealthStatus.CircuitOpen, _registry.GetPartitionHealth(3));

        var all = _registry.GetAllPartitionHealth();
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public void RegistryTracksFailureCountsCorrectly()
    {
        _registry.RecordFailure(1);
        _registry.RecordFailure(1);
        _registry.RecordFailure(1);

        Assert.Equal(3, _registry.GetFailureCount(1));

        _registry.ResetFailureCount(1);

        Assert.Equal(0, _registry.GetFailureCount(1));
    }
}
