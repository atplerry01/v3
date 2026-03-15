using System.Collections.Concurrent;
using Whycespace.Reliability.Isolation.Models;

namespace Whycespace.Reliability.Isolation.Monitor;

public sealed class WorkerHealthMonitor
{
    private const int UnhealthyThreshold = 5;

    private readonly ConcurrentDictionary<string, int> _consecutiveFailures = new();
    private readonly ConcurrentDictionary<string, WorkerHealthStatus> _workerHealth = new();

    public void RecordWorkerFailure(string workerId)
    {
        var failures = _consecutiveFailures.AddOrUpdate(workerId, 1, (_, count) => count + 1);

        if (failures >= UnhealthyThreshold)
        {
            _workerHealth[workerId] = WorkerHealthStatus.Unhealthy;
        }
        else if (failures >= 2)
        {
            _workerHealth[workerId] = WorkerHealthStatus.Degraded;
        }
    }

    public void RecordWorkerSuccess(string workerId)
    {
        _consecutiveFailures[workerId] = 0;
        _workerHealth[workerId] = WorkerHealthStatus.Healthy;
    }

    public WorkerHealthStatus GetWorkerHealth(string workerId)
    {
        return _workerHealth.GetValueOrDefault(workerId, WorkerHealthStatus.Healthy);
    }

    public int GetConsecutiveFailures(string workerId)
    {
        return _consecutiveFailures.GetValueOrDefault(workerId, 0);
    }

    public IReadOnlyDictionary<string, WorkerHealthStatus> GetAllWorkerHealth()
    {
        return new Dictionary<string, WorkerHealthStatus>(_workerHealth);
    }
}
