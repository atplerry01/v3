using System.Collections.Concurrent;
using Whycespace.Reliability.Isolation.Models;

namespace Whycespace.Reliability.Isolation.Registry;

public sealed class PartitionHealthRegistry
{
    private readonly ConcurrentDictionary<int, PartitionHealthStatus> _partitionHealth = new();
    private readonly ConcurrentDictionary<int, int> _failureCounts = new();
    private readonly ConcurrentDictionary<int, CircuitBreakerState> _circuitStates = new();

    public void SetPartitionHealth(int partitionId, PartitionHealthStatus status)
    {
        _partitionHealth[partitionId] = status;
    }

    public PartitionHealthStatus GetPartitionHealth(int partitionId)
    {
        return _partitionHealth.GetValueOrDefault(partitionId, PartitionHealthStatus.Healthy);
    }

    public void RecordFailure(int partitionId)
    {
        _failureCounts.AddOrUpdate(partitionId, 1, (_, count) => count + 1);
    }

    public int GetFailureCount(int partitionId)
    {
        return _failureCounts.GetValueOrDefault(partitionId, 0);
    }

    public void ResetFailureCount(int partitionId)
    {
        _failureCounts[partitionId] = 0;
    }

    public void SetCircuitState(int partitionId, CircuitBreakerState state)
    {
        _circuitStates[partitionId] = state;
    }

    public CircuitBreakerState GetCircuitState(int partitionId)
    {
        return _circuitStates.GetValueOrDefault(partitionId, CircuitBreakerState.Closed);
    }

    public IReadOnlyDictionary<int, PartitionHealthStatus> GetAllPartitionHealth()
    {
        return new Dictionary<int, PartitionHealthStatus>(_partitionHealth);
    }

    public IReadOnlyDictionary<int, CircuitBreakerState> GetAllCircuitStates()
    {
        return new Dictionary<int, CircuitBreakerState>(_circuitStates);
    }

    public IReadOnlyDictionary<int, int> GetAllFailureCounts()
    {
        return new Dictionary<int, int>(_failureCounts);
    }
}
