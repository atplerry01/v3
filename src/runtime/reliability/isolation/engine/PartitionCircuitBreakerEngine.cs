using Whycespace.Reliability.Isolation.Models;
using Whycespace.Reliability.Isolation.Registry;

namespace Whycespace.Reliability.Isolation.Engine;

public sealed class PartitionCircuitBreakerEngine
{
    private const int FailureThreshold = 10;
    private static readonly TimeSpan FailureWindow = TimeSpan.FromSeconds(30);

    private readonly PartitionHealthRegistry _registry;
    private readonly Dictionary<int, List<DateTime>> _failureTimestamps = new();
    private readonly object _lock = new();

    public PartitionCircuitBreakerEngine(PartitionHealthRegistry registry)
    {
        _registry = registry;
    }

    public void RecordPartitionFailure(int partitionId)
    {
        lock (_lock)
        {
            if (!_failureTimestamps.ContainsKey(partitionId))
            {
                _failureTimestamps[partitionId] = new List<DateTime>();
            }

            var now = DateTime.UtcNow;
            _failureTimestamps[partitionId].Add(now);

            var cutoff = now - FailureWindow;
            _failureTimestamps[partitionId].RemoveAll(t => t < cutoff);

            _registry.RecordFailure(partitionId);

            if (_failureTimestamps[partitionId].Count >= FailureThreshold)
            {
                OpenCircuit(partitionId);
            }
            else if (_failureTimestamps[partitionId].Count >= FailureThreshold / 2)
            {
                _registry.SetPartitionHealth(partitionId, PartitionHealthStatus.Degraded);
            }
        }
    }

    public void RecordPartitionSuccess(int partitionId)
    {
        lock (_lock)
        {
            var currentState = _registry.GetCircuitState(partitionId);

            if (currentState == CircuitBreakerState.HalfOpen)
            {
                CloseCircuit(partitionId);
            }
        }
    }

    public bool IsPartitionProcessingAllowed(int partitionId)
    {
        var state = _registry.GetCircuitState(partitionId);
        return state != CircuitBreakerState.Open;
    }

    private void OpenCircuit(int partitionId)
    {
        _registry.SetCircuitState(partitionId, CircuitBreakerState.Open);
        _registry.SetPartitionHealth(partitionId, PartitionHealthStatus.CircuitOpen);
    }

    private void CloseCircuit(int partitionId)
    {
        _registry.SetCircuitState(partitionId, CircuitBreakerState.Closed);
        _registry.SetPartitionHealth(partitionId, PartitionHealthStatus.Healthy);
        _registry.ResetFailureCount(partitionId);

        if (_failureTimestamps.ContainsKey(partitionId))
        {
            _failureTimestamps[partitionId].Clear();
        }
    }
}
