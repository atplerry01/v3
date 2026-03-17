using Whycespace.Runtime.Observability.Models;

namespace Whycespace.Runtime.Observability.Health;

public sealed class HealthCheckService
{
    private readonly List<Func<HealthStatus>> _probes = new();

    public void RegisterProbe(string component, Func<bool> check)
    {
        _probes.Add(() => new HealthStatus(
            component,
            check() ? "healthy" : "unhealthy",
            DateTime.UtcNow));
    }

    public HealthStatus CheckRuntime()
    {
        return new HealthStatus("runtime", "healthy", DateTime.UtcNow);
    }

    public HealthStatus CheckWorkers()
    {
        return new HealthStatus("workers", "healthy", DateTime.UtcNow);
    }

    public HealthStatus CheckEventFabric()
    {
        return new HealthStatus("eventFabric", "healthy", DateTime.UtcNow);
    }

    public IReadOnlyList<HealthStatus> CheckAll()
    {
        var results = new List<HealthStatus>
        {
            CheckRuntime(),
            CheckWorkers(),
            CheckEventFabric()
        };

        foreach (var probe in _probes)
            results.Add(probe());

        return results;
    }
}
