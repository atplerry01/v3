using Whycespace.Reliability.Isolation.Models;
using Whycespace.Reliability.Isolation.Registry;

namespace Whycespace.Reliability.Isolation.Engine;

public sealed class RuntimeRecoveryEngine
{
    private readonly PartitionHealthRegistry _registry;

    public RuntimeRecoveryEngine(PartitionHealthRegistry registry)
    {
        _registry = registry;
    }

    public void EvaluateRecovery(int partitionId)
    {
        var circuitState = _registry.GetCircuitState(partitionId);

        switch (circuitState)
        {
            case CircuitBreakerState.Open:
                TransitionToHalfOpen(partitionId);
                break;

            case CircuitBreakerState.HalfOpen:
                // HalfOpen allows limited processing; recovery is confirmed
                // by PartitionCircuitBreakerEngine on successful events
                break;

            case CircuitBreakerState.Closed:
                // Already healthy, no action needed
                break;
        }
    }

    public void EvaluateAllPartitions()
    {
        var allStates = _registry.GetAllCircuitStates();

        foreach (var (partitionId, state) in allStates)
        {
            if (state == CircuitBreakerState.Open)
            {
                EvaluateRecovery(partitionId);
            }
        }
    }

    public bool IsRecoveryCandidate(int partitionId)
    {
        return _registry.GetCircuitState(partitionId) == CircuitBreakerState.Open;
    }

    public CircuitBreakerState GetRecoveryState(int partitionId)
    {
        return _registry.GetCircuitState(partitionId);
    }

    private void TransitionToHalfOpen(int partitionId)
    {
        _registry.SetCircuitState(partitionId, CircuitBreakerState.HalfOpen);
        _registry.SetPartitionHealth(partitionId, PartitionHealthStatus.Degraded);
    }
}
