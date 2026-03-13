using Whycespace.Observability.Health;

namespace Whycespace.Observability.Tests;

public class HealthCheckTests
{
    [Fact]
    public void CheckRuntime_ReturnsHealthy()
    {
        var service = new HealthCheckService();

        var result = service.CheckRuntime();

        Assert.Equal("runtime", result.Component);
        Assert.Equal("healthy", result.Status);
    }

    [Fact]
    public void CheckAll_ReturnsAllComponents()
    {
        var service = new HealthCheckService();

        var results = service.CheckAll();

        Assert.Equal(3, results.Count);
        Assert.Contains(results, r => r.Component == "runtime");
        Assert.Contains(results, r => r.Component == "workers");
        Assert.Contains(results, r => r.Component == "eventFabric");
    }

    [Fact]
    public void RegisterProbe_IncludesCustomProbe()
    {
        var service = new HealthCheckService();
        service.RegisterProbe("database", () => true);

        var results = service.CheckAll();

        Assert.Equal(4, results.Count);
        Assert.Contains(results, r => r.Component == "database" && r.Status == "healthy");
    }

    [Fact]
    public void RegisterProbe_ReportsUnhealthy_WhenCheckFails()
    {
        var service = new HealthCheckService();
        service.RegisterProbe("database", () => false);

        var results = service.CheckAll();

        Assert.Contains(results, r => r.Component == "database" && r.Status == "unhealthy");
    }
}
